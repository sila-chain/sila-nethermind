// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Network.P2P.Subprotocols.Sil.V62;
using Nethermind.Network.P2P.Subprotocols.Sil.V63;
using Nethermind.Network.P2P.Subprotocols.Sil.V65;

namespace Nethermind.Network.P2P.Subprotocols.Sil.V66
{
    public static class Sil66MessageCode
    {
        public const int GetBlockHeaders = Sil62MessageCode.GetBlockHeaders;
        public const int BlockHeaders = Sil62MessageCode.BlockHeaders;
        public const int GetBlockBodies = Sil62MessageCode.GetBlockBodies;
        public const int BlockBodies = Sil62MessageCode.BlockBodies;
        public const int GetPooledTransactions = Sil65MessageCode.GetPooledTransactions;
        public const int PooledTransactions = Sil65MessageCode.PooledTransactions;
        public const int GetNodeData = Sil63MessageCode.GetNodeData;
        public const int NodeData = Sil63MessageCode.NodeData;
        public const int GetReceipts = Sil63MessageCode.GetReceipts;
        public const int Receipts = Sil63MessageCode.Receipts;
    }
}
