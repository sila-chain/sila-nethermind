// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages
{
    public class BlockHeadersMessage : Sil66Message<V62.Messages.BlockHeadersMessage>
    {
        public BlockHeadersMessage()
        {
        }

        public BlockHeadersMessage(long requestId, V62.Messages.BlockHeadersMessage silMessage) : base(requestId, silMessage)
        {
        }
    }
}
