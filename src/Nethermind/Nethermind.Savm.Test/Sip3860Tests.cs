// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Extensions;
using Nethermind.Specs;
using Nethermind.Savm.State;
using Nethermind.Core.Test.Builders;
using NUnit.Framework;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Savm.TransactionProcessing;

namespace Nethermind.Savm.Test
{
    public class Sip3860Tests : VirtualMachineTestsBase
    {
        protected override ulong BlockNumber => MainnetSpecProvider.ParisBlockNumber;
        protected override ulong Timestamp => MainnetSpecProvider.ShanghaiBlockTimestamp;

        private readonly ulong _transactionCallCost = GasCostOf.Transaction + 100 + 7 * GasCostOf.VeryLow;

        [TestCase("0x61013860006000f0", false, 32039)] //length 312
        [TestCase("0x61013860006000f0", true, 32059)] //extra 20 cost
        [TestCase("0x600261013860006000f5", false, 32102)] //length 312
        [TestCase("0x600261013860006000f5", true, 32122)] //extra 20 cost
        //cases from geth implementation
        [TestCase("0x61C00060006000f0", false, 41225)]
        [TestCase("0x61C00060006000f0", true, 44297)]
        [TestCase("0x600061C00060006000f5", false, 50444)]
        [TestCase("0x600061C00060006000f5", true, 53516)]
        public void Test_SIP_3860_GasCost_Create(string createCode, bool sip3860Enabled, long expectedGasUsage)
        {
            byte[] byteCode = Prepare.SavmCode
                .FromCode(createCode)
                .Done;

            TestState.CreateAccount(TestItem.AddressC, 1.Sila);
            TestState.InsertCode(TestItem.AddressC, byteCode, Spec);

            byte[] callCode = Prepare.SavmCode.Call(TestItem.AddressC, 100000).Done;

            TestAllTracerWithOutput tracer = Execute((BlockNumber, sip3860Enabled ? Timestamp : Timestamp - 1), callCode);
            Assert.That(tracer.StatusCode, Is.EqualTo(StatusCode.Success));
            Assert.That(tracer.GasSpent - _transactionCallCost, Is.EqualTo(expectedGasUsage));
        }

        [TestCase("60006000F0")]
        [TestCase("60006000F5")]
        public void Test_SIP_3860_InitCode_Create_Exceeds_Limit(string createCode)
        {
            string dataLengthHex = (Spec.MaxInitCodeSize + 1).ToString("X");
            Instruction dataPush = Instruction.PUSH1 + (byte)(dataLengthHex.Length / 2 - 1);

            bool isCreate2 = createCode[^2..] == Instruction.CREATE2.ToString("X");
            byte[] savmCode = isCreate2
                ? Prepare.SavmCode.PushSingle(0).FromCode(dataPush.ToString("X") + dataLengthHex + createCode).Done
                : Prepare.SavmCode.FromCode(dataPush.ToString("X") + dataLengthHex + createCode).Done;

            TestState.CreateAccount(TestItem.AddressC, 1.Sila);
            TestState.InsertCode(TestItem.AddressC, savmCode, Spec);

            const int contractCreationGasLimit = 50000;
            byte[] callCode = Prepare.SavmCode.Call(TestItem.AddressC, contractCreationGasLimit).Done;

            TestAllTracerWithOutput tracer = Execute(callCode);
            Assert.That(tracer.StatusCode, Is.EqualTo(StatusCode.Success));
            Assert.That(tracer.ReportedActionErrors.Count, Is.EqualTo(1));
            Assert.That(tracer.ReportedActionErrors[0], Is.EqualTo(SavmExceptionType.OutOfGas));
            Assert.That(TestState.GetNonce(TestItem.AddressC), Is.EqualTo(0));
            Assert.That(tracer.GasSpent, Is.EqualTo(_transactionCallCost + contractCreationGasLimit));
        }

        [Test]
        public void Test_SIP_3860_Disabled_InitCode_TxCreation_Exceeds_Limit_Succeeds()
        {
            (_, TestAllTracerWithOutput tracer) = PrepExecuteCreateTransaction(MainnetSpecProvider.ShanghaiBlockTimestamp - 1, Spec.MaxInitCodeSize + 1);

            Assert.That(tracer.StatusCode, Is.EqualTo(StatusCode.Success));
        }

        [Test]
        public void Test_SIP_3860_Enabled_InitCode_TxCreation_Exceeds_Limit_Fails()
        {
            (TransactionResult result, _) = PrepExecuteCreateTransaction(MainnetSpecProvider.ShanghaiBlockTimestamp, Spec.MaxInitCodeSize + 1);

            Assert.That(result, Is.EqualTo(TransactionResult.TransactionSizeOverMaxInitCodeSize));
        }

        [Test]
        public void Test_SIP_3860_Enabled_InitCode_TxCreation_Within_Limit_Succeeds()
        {
            //7680 is the size of create instructions - Prepare.SavmCode.Create
            (_, TestAllTracerWithOutput tracer) = PrepExecuteCreateTransaction(MainnetSpecProvider.ShanghaiBlockTimestamp, Spec.MaxInitCodeSize - 7680);

            Assert.That(tracer.StatusCode, Is.EqualTo(StatusCode.Success));
        }

        protected (TransactionResult, TestAllTracerWithOutput tracer) PrepExecuteCreateTransaction(ulong timestamp, long byteCodeSize)
        {
            byte[] byteCode = new byte[byteCodeSize];

            byte[] createCode = Prepare.SavmCode.Create(byteCode, 0).Done;

            TestState.CreateAccount(TestItem.AddressC, 1.Sila);

            (Block block, Transaction transaction) = PrepareTx((BlockNumber, timestamp), 500000, createCode);

            transaction.GasPrice = 2.GWei;
            transaction.To = null;
            transaction.Data = createCode;
            TestAllTracerWithOutput tracer = CreateTracer();
            TransactionResult result = _processor.Execute(transaction, new BlockExecutionContext(block.Header, SpecProvider.GetSpec(block.Header)), tracer);
            return (result, tracer);
        }
    }
}
