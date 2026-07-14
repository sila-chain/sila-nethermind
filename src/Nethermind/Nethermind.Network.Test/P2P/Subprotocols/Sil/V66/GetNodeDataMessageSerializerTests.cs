// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Network.P2P.Subprotocols.Sil.V66.Messages;
using NUnit.Framework;

namespace Nethermind.Network.Test.P2P.Subprotocols.Sil.V66
{
    [TestFixture]
    public class GetNodeDataMessageSerializerTests
    {
        //test from https://github.com/sila-chain/SIPs/blob/master/SIPS/sip-2481.md
        [Test]
        public void Roundtrip()
        {
            Hash256[] keys = { new("0x00000000000000000000000000000000000000000000000000000000deadc0de"), new("0x00000000000000000000000000000000000000000000000000000000feedbeef") };

            using Network.P2P.Subprotocols.Sil.V63.Messages.GetNodeDataMessage silMessage = new(keys.ToPooledList());

            GetNodeDataMessage message = new(1111, silMessage);

            GetNodeDataMessageSerializer serializer = new();

            SerializerTester.TestZero(serializer, message, "f847820457f842a000000000000000000000000000000000000000000000000000000000deadc0dea000000000000000000000000000000000000000000000000000000000feedbeef");
        }
    }
}
