// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Linq;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Savm.TransactionProcessing;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Specs;
using NUnit.Framework;

namespace Nethermind.Savm.Test
{
    // Alex Beregszaszi, Paweł Bylica, Andrei Maiboroda, Alexey Akhunov, Christian Reitwiessner, Martin Swende, "SIP-3541: Reject new contracts starting with the 0xEF byte," Sila Improvement Proposals, no. 3541, March 2021. [Online serial]. Available: https://sips.sila.org/SIPS/sip-3541.
    public class Sip3541Tests : VirtualMachineTestsBase
    {

        [Test]
        public void Wrong_contract_creation_should_return_invalid_code_after_3541(
            [ValueSource(nameof(Sip3541TestCases))] Sip3541TestCase test,
            [ValueSource(nameof(ContractDeployments))] ContractDeployment contractDeployment) => DeployCodeAndAssertTx(test.Code, true, contractDeployment, test.WithoutAnyInvalidCodeErrors);

        [Test]
        public void All_tx_should_pass_before_3541(
            [ValueSource(nameof(Sip3541TestCases))] Sip3541TestCase test,
            [ValueSource(nameof(ContractDeployments))] ContractDeployment contractDeployment) => DeployCodeAndAssertTx(test.Code, false, contractDeployment, true);

        public enum ContractDeployment
        {
            CREATE2,
            CREATE,
            InitCode
        }

        public static IEnumerable<ContractDeployment> ContractDeployments
        {
            get
            {
                yield return ContractDeployment.CREATE2;
                yield return ContractDeployment.CREATE;
                yield return ContractDeployment.InitCode;
            }
        }

        public class Sip3541TestCase
        {
            public string Code { get; set; }

            public bool WithoutAnyInvalidCodeErrors { get; set; }

            public override string ToString() =>
                $"Code: {Code}";
        }

        public static IEnumerable<Sip3541TestCase> Sip3541TestCases
        {
            get
            {
                yield return new Sip3541TestCase() { Code = "0x60ef60005360016000f3", WithoutAnyInvalidCodeErrors = false };
                yield return new Sip3541TestCase() { Code = "0x60ef60005360026000f3", WithoutAnyInvalidCodeErrors = false };
                yield return new Sip3541TestCase() { Code = "0x60ef60005360036000f3", WithoutAnyInvalidCodeErrors = false };
                yield return new Sip3541TestCase() { Code = "0x60ef60005360206000f3", WithoutAnyInvalidCodeErrors = false };
                yield return new Sip3541TestCase() { Code = "0x60fe60005360016000f3", WithoutAnyInvalidCodeErrors = true };
            }
        }


        void DeployCodeAndAssertTx(string code, bool sip3541Enabled, ContractDeployment context, bool withoutAnyInvalidCodeErrors)
        {
            TestState.CreateAccount(TestItem.AddressC, 100.Sila);

            byte[] salt = { 4, 5, 6 };
            byte[] byteCode = Prepare.SavmCode
                .FromCode(code)
                .Done;
            byte[] createContract = context switch
            {
                ContractDeployment.CREATE => Prepare.SavmCode.Create(byteCode, UInt256.Zero).Done,
                ContractDeployment.CREATE2 => Prepare.SavmCode.Create2(byteCode, salt, UInt256.Zero).Done,
                _ => byteCode,
            };
            _processor = new SilaTransactionProcessor(BlobBaseFeeCalculator.Instance, SpecProvider, TestState, Machine, CodeInfoRepository, LimboLogs.Instance);
            ulong blockNumber = sip3541Enabled ? MainnetSpecProvider.LondonBlockNumber : MainnetSpecProvider.LondonBlockNumber - 1;
            (Block block, Transaction transaction) = PrepareTx(blockNumber, 100000, createContract);

            transaction.GasPrice = 20.GWei;
            transaction.To = null;
            transaction.Data = createContract;
            TestAllTracerWithOutput tracer = CreateTracer();
            _processor.Execute(transaction, new BlockExecutionContext(block.Header, SpecProvider.GetSpec(block.Header)), tracer);

            Assert.That(tracer.ReportedActionErrors.All(static x => x != SavmExceptionType.InvalidCode), Is.EqualTo(withoutAnyInvalidCodeErrors), $"Code {code}, Context {context}");
        }
    }
}
