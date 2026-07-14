// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using NUnit.Framework;

namespace Nethermind.Core.Test
{
    [TestFixture]
    public class TransactionSipsSupportTests
    {
        [TestCase(TxType.AccessList, true, false, false)]
        [TestCase(TxType.SIP1559, true, true, false)]
        [TestCase(TxType.Blob, true, true, true)]
        public void When_sip_defines_new_tx_type_then_previous_sips_are_supported(TxType txType, bool isSip2930Supported,
            bool isSip1559Supported, bool isSip4844Supported)
        {
            Transaction transaction = new() { Type = txType };
            using (Assert.EnterMultipleScope())
            {
                Assert.That(transaction.SupportsAccessList, Is.EqualTo(isSip2930Supported));
                Assert.That(transaction.Supports1559, Is.EqualTo(isSip1559Supported));
                Assert.That(transaction.SupportsBlobs, Is.EqualTo(isSip4844Supported));
            }
        }
    }
}
