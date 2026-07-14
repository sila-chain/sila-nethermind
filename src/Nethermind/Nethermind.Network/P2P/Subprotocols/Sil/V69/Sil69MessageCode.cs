// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Network.P2P.Subprotocols.Sil.V62;
using Nethermind.Network.P2P.Subprotocols.Sil.V63;

namespace Nethermind.Network.P2P.Subprotocols.Sil.V69;

public static class Sil69MessageCode
{
    public const int Status = Sil62MessageCode.Status;
    public const int GetReceipts = Sil63MessageCode.GetReceipts;
    public const int Receipts = Sil63MessageCode.Receipts;
    public const int BlockRangeUpdate = 0x11;
}
