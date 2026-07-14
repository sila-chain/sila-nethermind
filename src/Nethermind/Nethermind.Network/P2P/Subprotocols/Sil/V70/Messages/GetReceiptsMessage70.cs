// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core.Collections;
using Nethermind.Core.Crypto;
using Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages;

namespace Nethermind.Network.P2P.Subprotocols.Sil.V70.Messages;

public class GetReceiptsMessage70(
    IOwnedReadOnlyList<Hash256> hashes,
    long firstBlockReceiptIndex = 0,
    bool generateRandomRequestId = true)
    : Sil66MessageBase(generateRandomRequestId)
{
    public IOwnedReadOnlyList<Hash256> Hashes { get; } = hashes ?? throw new ArgumentNullException(nameof(hashes));
    public long FirstBlockReceiptIndex { get; set; } = firstBlockReceiptIndex;

    public override int PacketType => Sil70MessageCode.GetReceipts;
    public override string Protocol => "sil";

    public GetReceiptsMessage70(long requestId, long firstBlockReceiptIndex, IOwnedReadOnlyList<Hash256> hashes)
        : this(hashes, firstBlockReceiptIndex, false) => RequestId = requestId;

    public override string ToString() => $"GetReceipts70({RequestId}, start={FirstBlockReceiptIndex}, {Hashes.Count})";

    public override void Dispose()
    {
        base.Dispose();
        Hashes.Dispose();
    }
}
