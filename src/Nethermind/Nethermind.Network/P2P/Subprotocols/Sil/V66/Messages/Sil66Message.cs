// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Network.P2P.Messages;

namespace Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages
{
    public abstract class Sil66Message<T> : P2PMessage, ISil66Message where T : P2PMessage
    {
        public override int PacketType => SilMessage.PacketType;
        public override string Protocol => SilMessage.Protocol;
        public long RequestId { get; set; } = MessageConstants.Random.NextLong();
        public T SilMessage { get; set; }

        protected Sil66Message()
        {
        }

        protected Sil66Message(long requestId, T silMessage)
        {
            RequestId = requestId;
            SilMessage = silMessage;
        }

        public override string ToString()
            => $"{GetType().Name}Sil66({RequestId},{SilMessage})";

        public override void Dispose()
        {
            base.Dispose();
            SilMessage.Dispose();
        }
    }

    public interface ISil66Message
    {
        long RequestId { get; }
    }
}
