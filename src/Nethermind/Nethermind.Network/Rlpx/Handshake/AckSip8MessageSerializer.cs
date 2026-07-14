// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using DotNetty.Buffers;
using Nethermind.Core.Crypto;
using Nethermind.Serialization.Rlp;

namespace Nethermind.Network.Rlpx.Handshake
{
    public class AckSip8MessageSerializer(IMessagePad messagePad) : IZeroMessageSerializer<AckSip8Message>
    {
        private readonly IMessagePad _messagePad = messagePad;
        public const int EphemeralPublicKeyLength = 64;
        public const int EphemeralPublicKeyOffset = 0;
        public const int NonceLength = 32;
        public const int NonceOffset = EphemeralPublicKeyOffset + EphemeralPublicKeyLength;
        public const int VersionOffset = NonceOffset + NonceLength;
        public const int TotalLength = EphemeralPublicKeyLength + NonceLength;

        public void Serialize(IByteBuffer byteBuffer, AckSip8Message msg)
        {
            int totalLength = Rlp.LengthOf(msg.EphemeralPublicKey.Bytes);
            totalLength += Rlp.LengthOf(msg.Nonce);
            totalLength += Rlp.LengthOf(msg.Version);

            byteBuffer.EnsureWritable(Rlp.LengthOfSequence(totalLength));
            ByteBufferRlpWriter writer = new(byteBuffer);
            writer.StartSequence(totalLength);
            writer.Encode(msg.EphemeralPublicKey.Bytes);
            writer.Encode(msg.Nonce);
            writer.Encode(msg.Version);
        }

        public AckSip8Message Deserialize(IByteBuffer msgBytes) =>
            msgBytes.DeserializeRlp(Deserialize);

        private static AckSip8Message Deserialize(ref RlpReader ctx)
        {
            AckSip8Message authSip8Message = new();
            ctx.ReadSequenceLength();
            authSip8Message.EphemeralPublicKey = new PublicKey(ctx.DecodeByteArraySpan(RlpLimit.L64));
            authSip8Message.Nonce = ctx.DecodeByteArray();
            return authSip8Message;
        }
    }
}
