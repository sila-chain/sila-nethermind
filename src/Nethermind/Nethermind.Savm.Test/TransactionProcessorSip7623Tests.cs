// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Blockchain;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test;
using Nethermind.Core.Test.Builders;
using Nethermind.Crypto;
using Nethermind.Savm.Tracing;
using Nethermind.Savm.TransactionProcessing;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Specs;
using Nethermind.Specs.Forks;
using Nethermind.Savm.State;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

public class TransactionProcessorSip7623Tests
{
    private ISpecProvider _specProvider;
    private ISilaEcdsa _silaEcdsa;
    private ITransactionProcessor _transactionProcessor;
    private IWorldState _stateProvider;
    private IDisposable _worldStateCloser;

    [SetUp]
    public void Setup()
    {
        _specProvider = new TestSpecProvider(SilaPrague.Instance);
        _stateProvider = TestWorldStateFactory.CreateForTest();
        _worldStateCloser = _stateProvider.BeginScope(IWorldState.PreGenesis);
        SilaCodeInfoRepository codeInfoRepository = new(_stateProvider);
        SilaVirtualMachine virtualMachine = new(new TestBlockhashProvider(_specProvider), _specProvider, LimboLogs.Instance);
        _transactionProcessor = new SilaTransactionProcessor(BlobBaseFeeCalculator.Instance, _specProvider, _stateProvider, virtualMachine, codeInfoRepository, LimboLogs.Instance);
        _silaEcdsa = new SilaEcdsa(_specProvider.ChainId);
    }

    [TearDown]
    public void TearDown() => _worldStateCloser?.Dispose();

    [TestCase(21006UL, false, TestName = "GasLimit=IntrinsicGas")]
    [TestCase(21010UL, true, TestName = "GasLimit=FloorGas")]

    public void transaction_validation_intrinsic_below_floor(ulong gasLimit, bool executed)
    {
        _stateProvider.CreateAccount(TestItem.AddressA, 1.Sila);
        _stateProvider.Commit(_specProvider.GenesisSpec);
        _stateProvider.CommitTree(0);

        Transaction tx = Build.A.Transaction
            .WithData([0])
            .WithGasPrice(1)
            .WithMaxFeePerGas(1)
            .WithTo(TestItem.AddressB)
            .WithValue(100.GWei)
            .WithGasLimit(gasLimit)
            .SignedAndResolved(_silaEcdsa, TestItem.PrivateKeyA)
            .TestObject;

        Block block = Build.A.Block.WithNumber(long.MaxValue)
            .WithTimestamp(MainnetSpecProvider.PragueBlockTimestamp)
            .WithTransactions(tx)
            .WithGasLimit(10000000).TestObject;

        TransactionResult result = _transactionProcessor.Execute(tx, new BlockExecutionContext(block.Header, _specProvider.GetSpec(block.Header)), NullTxTracer.Instance);
        Assert.That(result.TransactionExecuted, Is.EqualTo(executed));
    }

    [Test]
    public void balance_validation_intrinsic_below_floor()
    {
        _stateProvider.CreateAccount(TestItem.AddressA, 1.Sila);
        _stateProvider.Commit(_specProvider.GenesisSpec);
        _stateProvider.CommitTree(0);

        Transaction tx = Build.A.Transaction
            .WithData([0])
            .WithGasPrice(1)
            .WithMaxFeePerGas(1)
            .WithTo(TestItem.AddressB)
            .WithValue(100.GWei)
            .WithGasLimit(21010)
            .SignedAndResolved(_silaEcdsa, TestItem.PrivateKeyA)
            .TestObject;

        Block block = Build.A.Block.WithNumber(long.MaxValue)
            .WithTimestamp(MainnetSpecProvider.PragueBlockTimestamp)
            .WithTransactions(tx)
            .WithGasLimit(10000000).TestObject;

        _transactionProcessor.Execute(tx, new BlockExecutionContext(block.Header, _specProvider.GetSpec(block.Header)), NullTxTracer.Instance);

        UInt256 balance = _stateProvider.GetBalance(TestItem.AddressA);
        Assert.That(balance, Is.EqualTo(1.Sila - 100.GWei - 21010));
    }
}
