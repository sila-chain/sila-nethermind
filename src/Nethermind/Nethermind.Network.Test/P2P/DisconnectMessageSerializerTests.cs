// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Extensions;
using Nethermind.Network.P2P.Messages;
using Nethermind.Stats.Model;
using NUnit.Framework;

namespace Nethermind.Network.Test.P2P
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class DisconnectMessageSerializerTests
    {
        [Test]
        public void Can_do_roundtrip()
        {
            using DisconnectMessage msg = new(SilDisconnectReason.AlreadyConnected);
            DisconnectMessageSerializer serializer = new();
            byte[] serialized = serializer.Serialize(msg);
            Assert.That(serialized.ToHexString(true), Is.EqualTo("0xc105"), "bytes");
            using DisconnectMessage deserialized = serializer.Deserialize(serialized);
            Assert.That(deserialized.Reason, Is.EqualTo(msg.Reason), "reason");
        }

        [Test]
        public void Can_read_single_byte_message()
        {
            DisconnectMessageSerializer serializer = new();
            byte[] serialized = new byte[] { 16 };
            using DisconnectMessage deserialized = serializer.Deserialize(serialized);
            Assert.That((SilDisconnectReason)deserialized.Reason, Is.EqualTo(SilDisconnectReason.Other), "reason");
        }

        [TestCase("", SilDisconnectReason.DisconnectRequested)]
        [TestCase("00", SilDisconnectReason.DisconnectRequested)]
        [TestCase("10", SilDisconnectReason.Other)]
        [TestCase("82c104", SilDisconnectReason.TooManyPeers)]
        public void Can_read_other_format_message(string hex, SilDisconnectReason expectedReason)
        {
            DisconnectMessageSerializer serializer = new();
            byte[] serialized = Bytes.FromHexString(hex);
            using DisconnectMessage deserialized = serializer.Deserialize(serialized);
            Assert.That(deserialized.Reason, Is.EqualTo((int)expectedReason));
        }
    }
}
