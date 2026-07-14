// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;
using Nethermind.Specs;
using NUnit.Framework;

namespace Nethermind.Savm.Test
{
    [TestFixture]
    public class Sip1344Tests : VirtualMachineTestsBase
    {
        protected override ulong BlockNumber => MainnetSpecProvider.IstanbulBlockNumber;
        protected override ISpecProvider SpecProvider => MainnetSpecProvider.Instance;

        [Test]
        public void Chain_id_opcode_puts_expected_value_onto_the_stack()
        {
            ulong expectedChainId = SpecProvider.ChainId;

            byte[] code = Prepare.SavmCode
                .Op(Instruction.CHAINID)
                .PushData(0)
                .Op(Instruction.SSTORE)
                .Done;
            TestAllTracerWithOutput result = Execute(code);
            ulong setCost = expectedChainId == 0 ? GasCostOf.SStoreNetMeteredSip2200 : GasCostOf.SSet;
            Assert.That(result.StatusCode, Is.EqualTo(StatusCode.Success));
            AssertGas(result, 21000 + GasCostOf.VeryLow + GasCostOf.Base + setCost);
            AssertStorage(0, ((UInt256)expectedChainId).ToBigEndian());
        }
    }
}
