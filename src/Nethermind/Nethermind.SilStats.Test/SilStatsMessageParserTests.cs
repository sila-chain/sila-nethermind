// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using NUnit.Framework;

namespace Nethermind.SilStats.Test;

public class SilStatsMessageParserTests
{
    private static IEnumerable<TestCaseData> ParseCases()
    {
        yield return new TestCaseData(
            """{"emit":["history",{"min":1,"max":3}]}""",
            (int)SilStatsIncomingMessageType.History,
            1UL,
            3UL,
            null)
            .SetName("Can_parse_history_request");

        yield return new TestCaseData(
            """{"emit":["node-ping",{"clientTime":42}]}""",
            (int)SilStatsIncomingMessageType.NodePing,
            null,
            null,
            42L)
            .SetName("Can_parse_node_ping");

        yield return new TestCaseData(
            """{"emit":["node-pong",{"clientTime":42,"serverTime":84}]}""",
            (int)SilStatsIncomingMessageType.NodePong,
            null,
            null,
            42L)
            .SetName("Can_parse_node_pong");
    }

    [TestCaseSource(nameof(ParseCases))]
    public void Can_parse_message(
        string json,
        int expectedMessageType,
        ulong? expectedHistoryMin,
        ulong? expectedHistoryMax,
        long? expectedClientTime)
    {
        bool parsed = SilStatsMessageParser.TryParse(json, out SilStatsIncomingMessage message);
        SilStatsHistoryRequest? expectedHistoryRequest = expectedHistoryMin is null || expectedHistoryMax is null
            ? null
            : new SilStatsHistoryRequest(expectedHistoryMin.Value, expectedHistoryMax.Value);
        SilStatsNodeTiming? expectedNodeTiming = expectedClientTime is null
            ? null
            : new SilStatsNodeTiming(expectedClientTime);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed, Is.True);
            Assert.That(message.MessageType, Is.EqualTo((SilStatsIncomingMessageType)expectedMessageType));
            Assert.That(message.HistoryRequest, Is.EqualTo(expectedHistoryRequest));
            Assert.That(message.NodeTiming, Is.EqualTo(expectedNodeTiming));
        }
    }

    [Test]
    public void Ignores_invalid_payload()
    {
        bool parsed = SilStatsMessageParser.TryParse("""{"emit":["history",{"min":"bad","max":3}]}""", out _);

        Assert.That(parsed, Is.False);
    }
}
