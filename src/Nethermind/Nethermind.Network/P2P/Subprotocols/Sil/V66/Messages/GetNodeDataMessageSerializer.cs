// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages
{
    public class GetNodeDataMessageSerializer()
        : Sil66MessageSerializer<GetNodeDataMessage, V63.Messages.GetNodeDataMessage>(new V63.Messages.GetNodeDataMessageSerializer());
}
