// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Test.Builders;
using Nethermind.Crypto;
using Nethermind.Savm.State;
using Nethermind.Savm.Test.Helpers;
using Nethermind.Specs.Forks;
using NUnit.Framework;
using DbMetrics = Nethermind.Db.Metrics;

namespace Nethermind.Savm.Test;

/// <summary>
/// Verifies that SAVM execution metrics are correctly incremented during transaction processing.
/// </summary>
[TestFixture]
public class MetricsIntegrationTests
{
    private SavmTestHarness _harness = null!;

    [SetUp]
    public void Setup() => _harness = new SavmTestHarness();

    [TearDown]
    public void TearDown() => _harness.Dispose();

    [Test]
    public void ETH_transfer_increments_account_metrics()
    {
        PrivateKey sender = TestItem.PrivateKeyA;
        _harness.WorldState.CreateAccount(sender.Address, 10.Sila);

        long startReads = DbMetrics.MainThreadStateTreeReads;
        long startWrites = Metrics.MainThreadAccountWrites;

        Transaction tx = Build.A.Transaction.WithTo(TestItem.AddressB).WithValue(1.Sila)
            .WithGasLimit(21_000).SignedAndResolved(_harness.Ecdsa, sender, true).TestObject;
        _harness.ExecuteTx(tx, _harness.CreateBlock(tx));

        Assert.That(DbMetrics.MainThreadStateTreeReads - startReads, Is.GreaterThanOrEqualTo(2));
        Assert.That(Metrics.MainThreadAccountWrites - startWrites, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void SLOAD_increments_storage_read_metric()
    {
        PrivateKey sender = TestItem.PrivateKeyA;
        Address contract = TestItem.AddressC;
        _harness.WorldState.CreateAccount(sender.Address, 10.Sila);
        _harness.DeployCode(contract, Prepare.SavmCode.Op(Instruction.PUSH0).Op(Instruction.SLOAD).Op(Instruction.POP).Done);

        long startReads = DbMetrics.MainThreadStorageTreeReads + DbMetrics.MainThreadStorageTreeCache;

        Transaction tx = Build.A.Transaction.WithTo(contract).WithGasLimit(100_000)
            .SignedAndResolved(_harness.Ecdsa, sender, true).TestObject;
        _harness.ExecuteTx(tx, _harness.CreateBlock(tx));

        long currentReads = DbMetrics.MainThreadStorageTreeReads + DbMetrics.MainThreadStorageTreeCache;
        Assert.That(currentReads - startReads, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void SSTORE_increments_storage_write_metric()
    {
        PrivateKey sender = TestItem.PrivateKeyA;
        Address contract = TestItem.AddressC;
        _harness.WorldState.CreateAccount(sender.Address, 10.Sila);
        _harness.DeployCode(contract, Prepare.SavmCode.PushData(42).Op(Instruction.PUSH0).Op(Instruction.SSTORE).Done);

        long startWrites = Metrics.MainThreadStorageWrites;

        Transaction tx = Build.A.Transaction.WithTo(contract).WithGasLimit(100_000)
            .SignedAndResolved(_harness.Ecdsa, sender, true).TestObject;
        _harness.ExecuteTx(tx, _harness.CreateBlock(tx));

        Assert.That(Metrics.MainThreadStorageWrites - startWrites, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void SSTORE_zero_increments_storage_deleted_metric()
    {
        PrivateKey sender = TestItem.PrivateKeyA;
        Address contract = TestItem.AddressC;
        _harness.WorldState.CreateAccount(sender.Address, 10.Sila);
        _harness.DeployCode(contract, Prepare.SavmCode.Op(Instruction.PUSH0).Op(Instruction.PUSH0).Op(Instruction.SSTORE).Done);
        _harness.WorldState.Set(new StorageCell(contract, 0), new byte[] { 0x42 });
        _harness.WorldState.Commit(SilaPrague.Instance);

        long startDeleted = Metrics.MainThreadStorageDeleted;

        Transaction tx = Build.A.Transaction.WithTo(contract).WithGasLimit(100_000)
            .SignedAndResolved(_harness.Ecdsa, sender, true).TestObject;
        _harness.ExecuteTx(tx, _harness.CreateBlock(tx));

        Assert.That(Metrics.MainThreadStorageDeleted - startDeleted, Is.EqualTo(1));
    }

    [Test]
    public void Contract_creation_increments_code_write_metric()
    {
        PrivateKey sender = TestItem.PrivateKeyA;
        _harness.WorldState.CreateAccount(sender.Address, 10.Sila);

        long startCodeWrites = Metrics.MainThreadCodeWrites;

        byte[] initCode = Prepare.SavmCode.PushData(0).PushData(0).Op(Instruction.RETURN).Done;
        Transaction tx = Build.A.Transaction.WithCode(initCode).WithGasLimit(100_000)
            .SignedAndResolved(_harness.Ecdsa, sender, true).TestObject;
        _harness.ExecuteTx(tx, _harness.CreateBlock(tx));

        Assert.That(Metrics.MainThreadCodeWrites - startCodeWrites, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void SIP7702_delegation_set_increments_metric()
    {
        PrivateKey sender = TestItem.PrivateKeyA;
        PrivateKey signer = TestItem.PrivateKeyB;
        Address codeSource = TestItem.AddressC;
        _harness.WorldState.CreateAccount(sender.Address, 1.Sila);
        _harness.DeployCode(codeSource, Prepare.SavmCode.Op(Instruction.STOP).Done);

        long startSet = Metrics.MainThreadSip7702DelegationsSet;

        Transaction tx = Build.A.Transaction.WithType(TxType.SetCode).WithTo(signer.Address).WithGasLimit(100_000)
            .WithAuthorizationCode(_harness.Ecdsa.Sign(signer, _harness.SpecProvider.ChainId, codeSource, 0))
            .SignedAndResolved(_harness.Ecdsa, sender, true).TestObject;
        _harness.ExecuteTx(tx, _harness.CreateBlock(tx));

        Assert.That(Metrics.MainThreadSip7702DelegationsSet - startSet, Is.EqualTo(1));
    }

    [Test]
    public void SIP7702_delegation_clear_increments_metric()
    {
        PrivateKey sender = TestItem.PrivateKeyA;
        PrivateKey signer = TestItem.PrivateKeyB;
        _harness.WorldState.CreateAccount(sender.Address, 1.Sila);

        byte[] existingDelegation = new byte[23];
        Sip7702Constants.DelegationHeader.CopyTo(existingDelegation);
        TestItem.AddressC.Bytes.CopyTo(existingDelegation.AsSpan(3));
        _harness.WorldState.CreateAccount(signer.Address, 0);
        _harness.WorldState.InsertCode(signer.Address, existingDelegation, SilaPrague.Instance);
        _harness.WorldState.IncrementNonce(signer.Address);

        long startCleared = Metrics.MainThreadSip7702DelegationsCleared;

        Transaction tx = Build.A.Transaction.WithType(TxType.SetCode).WithTo(signer.Address).WithGasLimit(100_000)
            .WithAuthorizationCode(_harness.Ecdsa.Sign(signer, _harness.SpecProvider.ChainId, Address.Zero, 1))
            .SignedAndResolved(_harness.Ecdsa, sender, true).TestObject;
        _harness.ExecuteTx(tx, _harness.CreateBlock(tx));

        Assert.That(Metrics.MainThreadSip7702DelegationsCleared - startCleared, Is.EqualTo(1));
    }
}
