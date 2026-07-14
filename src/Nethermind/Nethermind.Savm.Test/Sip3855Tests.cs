// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Specs;
using NUnit.Framework;

namespace Nethermind.Savm.Test
{
    [TestFixture]
    public class Sip3855Tests : VirtualMachineTestsBase
    {
        protected override ulong BlockNumber => MainnetSpecProvider.ParisBlockNumber;
        protected override ulong Timestamp => MainnetSpecProvider.ShanghaiBlockTimestamp;

        private TestAllTracerWithOutput testBase(ulong repeat, bool isShanghai)
        {
            ulong timestampParam = isShanghai ? Timestamp : Timestamp - 1;
            Prepare codeInitializer = Prepare.SavmCode;
            for (ulong i = 0; i < repeat; i++)
            {
                codeInitializer.Op(Instruction.PUSH0);
            }

            byte[] code = codeInitializer.Done;
            TestAllTracerWithOutput receipt = Execute((BlockNumber, timestampParam), 1_000_000, code);
            return receipt;
        }

        [TestCase(0UL, true)]
        [TestCase(1UL, true)]
        [TestCase(123UL, true)]
        [TestCase(1024UL, true)]
        public void Test_Sip3855_should_pass(ulong repeat, bool isShanghai)
        {
            TestAllTracerWithOutput receipt = testBase(repeat, isShanghai);
            Assert.That(receipt.StatusCode, Is.EqualTo(StatusCode.Success));
            Assert.That(receipt.GasSpent, Is.EqualTo(repeat * GasCostOf.Base + GasCostOf.Transaction));
        }


        [TestCase(1UL, false, Description = "SilaShanghai fork deactivated")]
        [TestCase(123UL, false, Description = "SilaShanghai fork deactivated")]
        [TestCase(1234UL, false, Description = "SilaShanghai fork deactivated")]
        [TestCase(1025UL, true, Description = "SilaShanghai fork activated, stackoverflow")]
        [TestCase(1026UL, true, Description = "SilaShanghai fork activated, stackoverflow")]
        public void Test_Sip3855_should_fail(ulong repeat, bool isShanghai)
        {
            TestAllTracerWithOutput receipt = testBase(repeat, isShanghai);

            Assert.That(receipt.StatusCode, Is.EqualTo(StatusCode.Failure));

            if (isShanghai && repeat > 1024) // should fail because of stackoverflow (exceeds stack limit of 1024)
            {
                Assert.That(receipt.Error, Is.EqualTo(nameof(SavmExceptionType.StackOverflow)));
            }

            if (!isShanghai) // should fail because of bad instruction (push zero is an SIP-3540 new instruction)
            {
                Assert.That(receipt.Error, Is.EqualTo(nameof(SavmExceptionType.BadInstruction)));
            }
        }
    }
}
