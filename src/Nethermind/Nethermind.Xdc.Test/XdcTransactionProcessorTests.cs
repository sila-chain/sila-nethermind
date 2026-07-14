// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test;
using Nethermind.Core.Test.Builders;
using Nethermind.Savm;
using Nethermind.Savm.State;
using Nethermind.Savm.Tracing;
using Nethermind.Blockchain;
using Nethermind.Savm.TransactionProcessing;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Xdc.Contracts;
using Nethermind.Xdc.Spec;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Xdc.Test;

internal class XdcTransactionProcessorTests
{
    private IXdcReleaseSpec _spec;
    private ISpecProvider _specProvider;
    private IWorldState? _stateProvider;
    private IDisposable _worldStateCloser;
    private IMasternodeVotingContract _masternodeVotingContract;
    private TestXdcTransactionProcessor? _transactionProcessor;

    private static readonly UInt256 AccountBalance = 1.Sila;

    [SetUp]
    public void Setup()
    {
        _spec = Substitute.For<IXdcReleaseSpec>();
        _specProvider = Substitute.For<ISpecProvider>();
        _specProvider.GetSpec(Arg.Any<ForkActivation>()).Returns(_spec);
        _specProvider.GenesisSpec.Returns(_spec);

        _stateProvider = TestWorldStateFactory.CreateForTest();
        _worldStateCloser = _stateProvider.BeginScope(IWorldState.PreGenesis);
        _stateProvider.CreateAccount(TestItem.AddressA, AccountBalance);
        _stateProvider.Commit(_spec);
        _stateProvider.CommitTree(0);

        _masternodeVotingContract = Substitute.For<IMasternodeVotingContract>();

        SilaCodeInfoRepository codeInfoRepository = new(_stateProvider);
        SilaVirtualMachine virtualMachine = new(new TestBlockhashProvider(_specProvider), _specProvider, LimboLogs.Instance);
        _transactionProcessor = new TestXdcTransactionProcessor(
            BlobBaseFeeCalculator.Instance,
            _specProvider,
            _stateProvider,
            virtualMachine,
            codeInfoRepository,
            LimboLogs.Instance,
            _masternodeVotingContract);
    }

    [TearDown]
    public void TearDown() =>
        _worldStateCloser.Dispose();

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void PayFees_IsTipTrc21FeeEnabled_ShouldPayFeesToTheCorrectAddress(
        bool tipTrc21FeeEnabled,
        bool isSip1559Enabled)
    {
        _spec.IsTipTrc21FeeEnabled.Returns(tipTrc21FeeEnabled);
        _spec.IsSip1559Enabled.Returns(isSip1559Enabled);

        ulong spentGas = 21000;
        UInt256 premiumPerGas = 9;
        UInt256 blobBaseFee = 0;
        Address beneficiaryAddress = TestItem.AddressB;
        Address ownerAddress = TestItem.AddressD;

        _stateProvider!.CreateAccount(beneficiaryAddress, AccountBalance);
        _stateProvider.CreateAccount(ownerAddress, UInt256.Zero);

        _masternodeVotingContract.GetCandidateOwner(Arg.Any<IWorldState>(), beneficiaryAddress)
            .Returns(ownerAddress);

        Transaction tx = Build.A.Transaction
            .WithMaxFeePerGas(10)
            .WithMaxPriorityFeePerGas(9)
            .WithGasLimit(21000)
            .WithType(isSip1559Enabled ? TxType.SIP1559 : TxType.Legacy)
            .TestObject;

        XdcBlockHeader header = Build.A.XdcBlockHeader()
            .WithNumber(1)
            .WithBaseFee(1)
            .TestObject;
        header.Beneficiary = beneficiaryAddress;

        TransactionSubstate substate = default;

        _transactionProcessor!.SetBlockExecutionContext(header);

        FeesTracer tracer = new();
        UInt256 initialBeneficiaryBalance = _stateProvider.GetBalance(beneficiaryAddress);
        UInt256 initialOwnerBalance = _stateProvider.GetBalance(ownerAddress);

        _transactionProcessor!.TestPayFees(tx, header, _spec, tracer, substate, spentGas, premiumPerGas, blobBaseFee, StatusCode.Success);

        UInt256 finalBeneficiaryBalance = _stateProvider.GetBalance(beneficiaryAddress);
        UInt256 finalOwnerBalance = _stateProvider.GetBalance(ownerAddress);
        UInt256 beneficiaryReceivedFees = finalBeneficiaryBalance - initialBeneficiaryBalance;
        UInt256 ownerReceivedFees = finalOwnerBalance - initialOwnerBalance;

        if (tipTrc21FeeEnabled)
        {
            UInt256 effectiveGasPrice = tx.CalculateEffectiveGasPrice(_spec.IsSip1559Enabled, header.BaseFeePerGas);
            UInt256 expectedFees = effectiveGasPrice * spentGas;
            Assert.That(ownerReceivedFees, Is.EqualTo(expectedFees));
            Assert.That(beneficiaryReceivedFees, Is.EqualTo(UInt256.Zero));
        }
        else
        {
            UInt256 expectedFees = premiumPerGas * spentGas;
            Assert.That(beneficiaryReceivedFees, Is.EqualTo(expectedFees));
            Assert.That(ownerReceivedFees, Is.EqualTo(UInt256.Zero));
        }
    }

    private class TestXdcTransactionProcessor(
        ITransactionProcessor.IBlobBaseFeeCalculator blobBaseFeeCalculator,
        ISpecProvider? specProvider,
        IWorldState? worldState,
        IVirtualMachine? virtualMachine,
        ICodeInfoRepository? codeInfoRepository,
        ILogManager? logManager,
        IMasternodeVotingContract masternodeVotingContract) : XdcTransactionProcessor(blobBaseFeeCalculator, specProvider, worldState, virtualMachine, codeInfoRepository, logManager, masternodeVotingContract)
    {
        public void TestPayFees(
            Transaction tx,
            XdcBlockHeader header,
            IReleaseSpec spec,
            ITxTracer tracer,
            in TransactionSubstate substate,
            ulong spentGas,
            in UInt256 premiumPerGas,
            in UInt256 blobBaseFee,
            int statusCode) =>
            PayFees(tx, header, spec, tracer, substate, spentGas, premiumPerGas, blobBaseFee, statusCode);
    }
}

