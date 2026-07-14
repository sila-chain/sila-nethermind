// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages
{
    public class PooledTransactionsMessage : Sil66Message<V65.Messages.PooledTransactionsMessage>
    {
        public PooledTransactionsMessage()
        {
        }

        public PooledTransactionsMessage(long requestId, V65.Messages.PooledTransactionsMessage silMessage) : base(requestId, silMessage)
        {
        }
    }
}
