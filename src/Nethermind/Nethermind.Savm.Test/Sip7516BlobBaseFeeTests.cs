// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Savm.TransactionProcessing;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Specs;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

[TestFixture]
public class Sip7516BlobBaseFeeTests : VirtualMachineTestsBase
{

    [TestCase(true, 0ul)]
    [TestCase(true, 100ul)]
    [TestCase(true, 20ul)]
    [TestCase(false, 20ul)]
    [TestCase(false, 0ul)]
    public void Blob_Base_fee_opcode_should_return_expected_results(bool sip7516Enabled, ulong excessBlobGas)
    {
        _processor = new SilaTransactionProcessor(BlobBaseFeeCalculator.Instance, SpecProvider, TestState, Machine, CodeInfoRepository, LimboLogs.Instance);
        byte[] code = Prepare.SavmCode
            .Op(Instruction.BLOBBASEFEE)
            .PushData(0)
            .Op(Instruction.SSTORE)
            .Done;

        ForkActivation activation = sip7516Enabled ? MainnetSpecProvider.CancunActivation : MainnetSpecProvider.ShanghaiActivation;
        (Block block, Transaction transaction) = PrepareTx(activation, 100000, code);
        block.Header.ExcessBlobGas = excessBlobGas;

        IReleaseSpec spec = SpecProvider.GetSpec(activation);
        TestAllTracerWithOutput tracer = CreateTracer();
        _processor.Execute(transaction, new BlockExecutionContext(block.Header, spec), tracer);
        _ = BlobGasCalculator.TryCalculateFeePerBlobGas(excessBlobGas, spec.BlobBaseFeeUpdateFraction, out UInt256 expectedFeePerBlobGas);
        if (sip7516Enabled)
        {
            AssertStorage((UInt256)0, expectedFeePerBlobGas);
        }
        else
        {
            Assert.That(tracer.Error, Is.EqualTo(SavmExceptionType.BadInstruction.ToString()));
            AssertStorage((UInt256)0, (UInt256)0);
        }
    }
}
