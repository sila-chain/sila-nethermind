// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Collections;
using Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages;
using NUnit.Framework;

namespace Nethermind.Network.Test.P2P.Subprotocols.Sil.V66
{
    [TestFixture]
    public class NodeDataMessageSerializerTests
    {
        //test from https://github.com/sila-chain/SIPs/blob/master/SIPS/sip-2481.md
        [Test]
        public void Roundtrip()
        {
            ArrayPoolList<byte[]> data = new(2) { new byte[] { 0xde, 0xad, 0xc0, 0xde }, new byte[] { 0xfe, 0xed, 0xbe, 0xef } };
            using Network.P2P.Subprotocols.Sil.V63.Messages.NodeDataMessage silMessage = new(new ByteArrayListAdapter(data));

            NodeDataMessage message = new(1111, silMessage);

            NodeDataMessageSerializer serializer = new();

            SerializerTester.TestZero(serializer, message, "ce820457ca84deadc0de84feedbeef");
        }
    }
}
