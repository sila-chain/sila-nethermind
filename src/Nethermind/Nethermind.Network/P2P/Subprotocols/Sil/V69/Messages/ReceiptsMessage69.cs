// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Network.P2P.Subprotocols.Sil.V69.Messages
{
    public class ReceiptsMessage69 : Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages.ReceiptsMessage
    {
        public ReceiptsMessage69() { }

        public ReceiptsMessage69(long requestId, V63.Messages.ReceiptsMessage silMessage) : base(requestId, silMessage) { }
    }
}
