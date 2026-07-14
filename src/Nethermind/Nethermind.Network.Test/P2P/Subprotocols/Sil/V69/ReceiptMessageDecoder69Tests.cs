// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Network.P2P.Subprotocols.Sil.V69.Messages;
using Nethermind.Serialization.Rlp;
using NUnit.Framework;

namespace Nethermind.Network.Test.P2P.Subprotocols.Sil.V69;

[TestFixture]
public class ReceiptMessageDecoder69Tests
{
    [Test]
    public void Can_roundtrip_receipt()
    {
        TxReceipt receipt = new()
        {
            TxType = TxType.SIP1559,
            StatusCode = 1,
            GasUsedTotal = 21000,
            Bloom = new Bloom(),
            Logs = []
        };

        ReceiptMessageDecoder69 decoder = new();
        int length = decoder.GetLength(receipt, RlpBehaviors.Sip658Receipts);
        byte[] encoded = new byte[length];
        RlpWriter writer = new(encoded);
        decoder.Encode(ref writer, receipt, RlpBehaviors.Sip658Receipts);

        RlpReader context = new(encoded);
        TxReceipt? decoded = decoder.Decode(ref context, RlpBehaviors.Sip658Receipts);

        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded!.TxType, Is.EqualTo(receipt.TxType));
        Assert.That(decoded.StatusCode, Is.EqualTo(receipt.StatusCode));
        Assert.That(decoded.GasUsedTotal, Is.EqualTo(receipt.GasUsedTotal));
    }
}
