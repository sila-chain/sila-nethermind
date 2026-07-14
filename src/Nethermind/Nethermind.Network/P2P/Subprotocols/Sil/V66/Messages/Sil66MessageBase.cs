// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Network.P2P.Messages;

namespace Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages
{
    /// <summary>
    /// Base type for sil request/response messages that carry a request id on the wire.
    /// </summary>
    public abstract class Sil66MessageBase(bool generateRandomRequestId = true) : P2PMessage, ISil66Message
    {
        public long RequestId { get; set; } = generateRandomRequestId ? MessageConstants.Random.NextLong() : 0;
    }
}
