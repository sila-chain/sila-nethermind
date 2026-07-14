// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.SilStats.Clients;
using Nethermind.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.SilStats.Test
{
    public class SilStatsClientTests
    {
        [TestCase("https://localhost/api", "wss://localhost/api")]
        [TestCase("wss://localhost/api", "wss://localhost/api")]
        [TestCase("ws://localhost/api", "ws://localhost/api")]
        [TestCase("http://localhost/api", "ws://localhost/api")]
        [TestCase("https://localhost:8000/api", "wss://localhost:8000/api")]
        [TestCase("http://test://", "ws://test//")]
        public void Build_url_should_return_expected_results(string configUrl, string expectedUrl)
        {
            SilStatsClient silClient = new(configUrl, 5000, Substitute.For<IMessageSender>(), LimboLogs.Instance);
            Assert.That(silClient.BuildUrl(), Is.EqualTo(expectedUrl));
        }

        [TestCase("http://test:://")]
        [TestCase("ftp://localhost")]
        [TestCase("http:/")]
        [TestCase("localhost")]
        public void Incorrect_url_should_throw_exception(string url)
        {
            SilStatsClient silClient = new(url, 5000, Substitute.For<IMessageSender>(), LimboLogs.Instance);
            Assert.Throws<ArgumentException>(() => silClient.BuildUrl());
        }
    }
}
