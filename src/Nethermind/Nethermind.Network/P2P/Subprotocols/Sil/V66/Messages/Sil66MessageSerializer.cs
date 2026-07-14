// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DotNetty.Buffers;
using Nethermind.Network.P2P.Messages;
using Nethermind.Serialization.Rlp;

namespace Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages
{
    public class Sil66MessageSerializer<TSil66Message, TSilMessage> : IZeroInnerMessageSerializer<TSil66Message>
        where TSil66Message : Sil66Message<TSilMessage>, new()
        where TSilMessage : P2PMessage
    {
        private readonly IZeroInnerMessageSerializer<TSilMessage> _ethMessageSerializer;

        protected Sil66MessageSerializer(IZeroInnerMessageSerializer<TSilMessage> silMessageSerializer) => _ethMessageSerializer = silMessageSerializer;

        public void Serialize(IByteBuffer byteBuffer, TSil66Message message)
        {
            int length = GetLength(message, out int contentLength);
            byteBuffer.EnsureWritable(length);
            ByteBufferRlpWriter writer = new(byteBuffer);
            writer.StartSequence(contentLength);
            writer.Encode(message.RequestId);
            _ethMessageSerializer.Serialize(byteBuffer, message.SilMessage);
        }

        public TSil66Message Deserialize(IByteBuffer byteBuffer)
        {
            int startReaderIndex = byteBuffer.ReaderIndex;
            RlpReader ctx = new(byteBuffer.AsSpan());
            int sequenceLength = ctx.ReadSequenceLength();
            int checkPosition = ctx.Position + sequenceLength;
            TSil66Message sil66Message = new();
            sil66Message.RequestId = ctx.DecodeLong();
            byteBuffer.SetReaderIndex(byteBuffer.ReaderIndex + ctx.Position);
            sil66Message.SilMessage = _ethMessageSerializer.Deserialize(byteBuffer);

            if (byteBuffer.ReaderIndex - startReaderIndex != checkPosition)
            {
                ThrowUnexpectedTrailingData();
            }

            return sil66Message;
        }

        public int GetLength(TSil66Message message, out int contentLength)
        {
            int innerMessageLength = _ethMessageSerializer.GetLength(message.SilMessage, out _);
            contentLength =
                Rlp.LengthOf(message.RequestId) +
                innerMessageLength;

            return Rlp.LengthOfSequence(contentLength);
        }

        [DoesNotReturn, StackTraceHidden]
        private static void ThrowUnexpectedTrailingData() =>
            throw new RlpException("Unexpected trailing data in sil66 message");
    }
}
