// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using DotNetty.Buffers;
using Nethermind.Core.Specs;
using Nethermind.Network.P2P.Subprotocols.Sil.V63.Messages;

namespace Nethermind.Network.P2P.Subprotocols.Sil.V69.Messages;

/// <remarks>
/// "Inner" serializer here inherits and overrides parts of sil/63 implementation,
/// while <see cref="ReceiptsMessageSerializer69"/> "wraps" it after, similar to sil/66 version.
/// </remarks>
public class ReceiptsMessageInnerSerializer69(ISpecProvider specProvider) :
    ReceiptsMessageSerializer(specProvider, new ReceiptMessageDecoder69()),
    IZeroInnerMessageSerializer<ReceiptsInnerMessage69>
{
    int IZeroInnerMessageSerializer<ReceiptsInnerMessage69>.GetLength(ReceiptsInnerMessage69 message, out int contentLength) =>
        GetLength(message, out contentLength);

    void IZeroMessageSerializer<ReceiptsInnerMessage69>.Serialize(IByteBuffer byteBuffer, ReceiptsInnerMessage69 message) =>
        Serialize(byteBuffer, message);

    ReceiptsInnerMessage69 IZeroMessageSerializer<ReceiptsInnerMessage69>.Deserialize(IByteBuffer byteBuffer)
    {
        ReceiptsMessage baseMessage = base.Deserialize(byteBuffer);
        return new(baseMessage.TxReceipts);
    }
}
