// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Specs;
using Nethermind.Savm.State;
using Nethermind.Core.Test.Builders;
using Nethermind.Int256;
using NUnit.Framework;

namespace Nethermind.Savm.Test
{
    public class Sip1884Tests : VirtualMachineTestsBase
    {
        protected override ulong BlockNumber => MainnetSpecProvider.IstanbulBlockNumber;
        protected override ISpecProvider SpecProvider => MainnetSpecProvider.Instance;

        private TestAllTracerWithOutput Execute(bool isIstanbul, byte[] code) =>
            isIstanbul ? Execute(code) : Execute(BlockNumber - 1, 100000, code);

        [Test]
        public void after_istanbul_selfbalance_opcode_puts_current_address_balance_onto_the_stack()
        {
            byte[] contractCode = Prepare.SavmCode
                .Op(Instruction.SELFBALANCE)
                .PushData(0)
                .Op(Instruction.SSTORE)
                .Done;

            TestState.CreateAccount(TestItem.AddressC, 1.Sila);
            TestState.InsertCode(TestItem.AddressC, contractCode, Spec);

            TestState.CreateAccount(TestItem.AddressD, 1.Sila);
            TestState.InsertCode(TestItem.AddressD, contractCode, Spec);

            byte[] code = Prepare.SavmCode
                .Call(TestItem.AddressC, 50000)
                .DelegateCall(TestItem.AddressD, 50000)
                .Op(Instruction.SELFBALANCE)
                .PushData(1)
                .Op(Instruction.SSTORE)
                .Done;

            TestAllTracerWithOutput result = Execute(code);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.Success));
            AssertGas(result, 21000 + 2 * GasCostOf.CallSip150 + 24 + 21 + GasCostOf.VeryLow + 3 * GasCostOf.SelfBalance + 3 * GasCostOf.SSet);
            UInt256 balanceB = TestState.GetBalance(TestItem.AddressB);
            UInt256 balanceC = TestState.GetBalance(TestItem.AddressC);
            AssertStorage(new StorageCell(TestItem.AddressB, UInt256.Zero), balanceB);
            AssertStorage(new StorageCell(TestItem.AddressB, UInt256.One), balanceB);
            AssertStorage(new StorageCell(TestItem.AddressC, UInt256.Zero), balanceC);
        }

        [TestCase(true, GasCostOf.ExtCodeHashSip1884)]
        [TestCase(false, GasCostOf.ExtCodeHash)]
        public void Extcodehash_cost_depends_on_istanbul(bool isIstanbul, ulong expectedOpGasCost)
        {
            TestState.CreateAccount(TestItem.AddressC, 100.Sila);

            byte[] code = Prepare.SavmCode
                .PushData(TestItem.AddressC)
                .Op(Instruction.EXTCODEHASH)
                .Done;

            TestAllTracerWithOutput result = Execute(isIstanbul, code);
            AssertGas(result, 21000 + GasCostOf.VeryLow + expectedOpGasCost);
        }

        [TestCase(true, GasCostOf.BalanceSip1884)]
        [TestCase(false, GasCostOf.BalanceSip150)]
        public void Balance_cost_depends_on_istanbul(bool isIstanbul, ulong expectedOpGasCost)
        {
            TestState.CreateAccount(TestItem.AddressC, 100.Sila);

            byte[] code = Prepare.SavmCode
                .PushData(TestItem.AddressC)
                .Op(Instruction.BALANCE)
                .Done;

            TestAllTracerWithOutput result = Execute(isIstanbul, code);
            AssertGas(result, 21000 + GasCostOf.VeryLow + expectedOpGasCost);
        }

        [TestCase(true, GasCostOf.SLoadSip1884)]
        [TestCase(false, GasCostOf.SLoadSip150)]
        public void Sload_cost_depends_on_istanbul(bool isIstanbul, ulong expectedOpGasCost)
        {
            TestState.CreateAccount(TestItem.AddressC, 100.Sila);

            byte[] code = Prepare.SavmCode
                .PushData(TestItem.AddressC)
                .PushData(0)
                .Op(Instruction.SLOAD)
                .Done;

            TestAllTracerWithOutput result = Execute(isIstanbul, code);
            AssertGas(result, 21000 + 2 * GasCostOf.VeryLow + expectedOpGasCost);
        }
    }
}
