// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages
{
    public class ReceiptsMessage : Sil66Message<V63.Messages.ReceiptsMessage>
    {
        public ReceiptsMessage()
        {
        }

        public ReceiptsMessage(long requestId, V63.Messages.ReceiptsMessage silMessage) : base(requestId, silMessage)
        {
        }
    }
}
