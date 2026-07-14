// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using Nethermind.Core;
using Nethermind.Int256;
using NUnit.Framework;

namespace Nethermind.TxPool.Test
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class TransactionExtensionsTests
    {
        [Test]
        public void CalculatePayableGasPrice_returns_expected_results([ValueSource(nameof(TransactionPayableGasPriceCases))]
            TransactionPayableGasPrice test)
        {
            Transaction tx = new();
            tx.Type = test.Type;
            tx.GasPrice = test.GasPrice;
            tx.GasLimit = test.GasLimit;
            tx.Value = test.Value;
            tx.DecodedMaxFeePerGas = test.FeeCap;

            UInt256 payableGasPrice = tx.CalculateAffordableGasPrice(test.IsSip1559Enabled, test.BaseFee, test.AccountBalance);
            Assert.That(payableGasPrice, Is.EqualTo(test.ExpectedPayableGasPriceResult));
        }

        public class TransactionPayableGasPrice
        {
            public int Lp { get; set; }
            public UInt256 BaseFee { get; set; }
            public ulong FeeCap { get; set; }
            public UInt256 GasPrice { get; set; }
            public TxType Type { get; set; }
            public ulong GasLimit { get; set; }
            public ulong Value { get; set; }
            public bool IsSip1559Enabled { get; set; }

            public UInt256 AccountBalance { get; set; }
            public UInt256 ExpectedPayableGasPriceResult { get; set; }

            public override string ToString() =>
                $"Lp: {Lp}, ExpectedPayableGasPriceResult: {ExpectedPayableGasPriceResult}";
        }

        public static IEnumerable<TransactionPayableGasPrice> TransactionPayableGasPriceCases
        {
            get
            {
                /* Legacy transactions before 1559 fork:*/
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 1,
                    GasPrice = 10,
                    AccountBalance = 100,
                    ExpectedPayableGasPriceResult = 10
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 2,
                    GasPrice = 21,
                    GasLimit = 100,
                    AccountBalance = 2100,
                    ExpectedPayableGasPriceResult = 21
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 3,
                    GasPrice = 21,
                    GasLimit = 100,
                    Value = 3,
                    AccountBalance = 2100,
                    ExpectedPayableGasPriceResult = 21
                };

                /*Legacy after 1559 fork:*/
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 4,
                    IsSip1559Enabled = true,
                    GasPrice = 10,
                    GasLimit = 300,
                    Value = 5,
                    AccountBalance = 3005,
                    ExpectedPayableGasPriceResult = 10
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 5,
                    IsSip1559Enabled = true,
                    GasPrice = 10,
                    GasLimit = 300,
                    Value = 5,
                    BaseFee = 500,
                    AccountBalance = 3005,
                    ExpectedPayableGasPriceResult = 10
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 6,
                    IsSip1559Enabled = true,
                    GasPrice = 10,
                    GasLimit = 300,
                    Value = 5,
                    BaseFee = 5,
                    AccountBalance = 3004,
                    ExpectedPayableGasPriceResult = 10
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 7,
                    IsSip1559Enabled = true,
                    GasPrice = 0,
                    GasLimit = 300,
                    Value = 0,
                    BaseFee = 5,
                    AccountBalance = 100000,
                    ExpectedPayableGasPriceResult = 0
                };

                /* Sip1559 transactions before 1559 fork:*/
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 8,
                    Type = TxType.SIP1559,
                    GasPrice = 10,
                    GasLimit = 300,
                    FeeCap = 500,
                    Value = 5,
                    AccountBalance = 3005,
                    ExpectedPayableGasPriceResult = 10
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 9,
                    Type = TxType.SIP1559,
                    GasPrice = 10,
                    GasLimit = 300,
                    Value = 5,
                    AccountBalance = 3004,
                    ExpectedPayableGasPriceResult = 10
                };

                /* Sip1559 transactions after 1559 fork:*/
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 10,
                    IsSip1559Enabled = true,
                    Type = TxType.SIP1559,
                    GasPrice = 10,
                    GasLimit = 300,
                    FeeCap = 500,
                    Value = 5,
                    BaseFee = 20,
                    AccountBalance = 10000,
                    ExpectedPayableGasPriceResult = 30
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 11,
                    IsSip1559Enabled = true,
                    Type = TxType.SIP1559,
                    GasPrice = 10,
                    GasLimit = 300,
                    FeeCap = 500,
                    Value = 5,
                    BaseFee = 20,
                    AccountBalance = 2000,
                    ExpectedPayableGasPriceResult = 6
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 12,
                    IsSip1559Enabled = true,
                    Type = TxType.SIP1559,
                    GasPrice = 10,
                    GasLimit = 300,
                    FeeCap = 500,
                    Value = 5,
                    BaseFee = 20,
                    AccountBalance = 305,
                    ExpectedPayableGasPriceResult = 1
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 13,
                    IsSip1559Enabled = true,
                    Type = TxType.SIP1559,
                    GasPrice = 10,
                    GasLimit = 300,
                    FeeCap = 500,
                    Value = 5,
                    BaseFee = 20,
                    AccountBalance = 304,
                    ExpectedPayableGasPriceResult = 0
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 14,
                    IsSip1559Enabled = true,
                    Type = TxType.SIP1559,
                    GasPrice = 0,
                    GasLimit = 300,
                    FeeCap = 500,
                    Value = 5,
                    BaseFee = 20,
                    AccountBalance = 10000,
                    ExpectedPayableGasPriceResult = 20
                };
                yield return new TransactionPayableGasPrice()
                {
                    Lp = 15,
                    IsSip1559Enabled = true,
                    Type = TxType.SIP1559,
                    GasPrice = 0,
                    GasLimit = 300,
                    FeeCap = 10,
                    Value = 5,
                    BaseFee = 20,
                    AccountBalance = 10000,
                    ExpectedPayableGasPriceResult = 10
                };
            }
        }
    }
}
