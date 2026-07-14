// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages
{
    public class GetNodeDataMessage : Sil66Message<V63.Messages.GetNodeDataMessage>
    {
        public GetNodeDataMessage()
        {
        }

        public GetNodeDataMessage(long requestId, V63.Messages.GetNodeDataMessage silMessage) : base(requestId, silMessage)
        {
        }
    }
}
