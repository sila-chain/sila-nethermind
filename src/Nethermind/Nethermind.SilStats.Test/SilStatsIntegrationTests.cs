// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nethermind.Blockchain;
using Nethermind.Core.Test.Builders;
using Nethermind.SilStats.Integrations;
using Nethermind.SilStats.Messages;
using Nethermind.Facade.Sil;
using Nethermind.JsonRpc.Modules.Sil.GasPrice;
using Nethermind.Logging;
using Nethermind.Network;
using Nethermind.TxPool;
using NSubstitute;
using NUnit.Framework;
using Websocket.Client;
using CoreBlock = Nethermind.Core.Block;
using SilStatsBlock = Nethermind.SilStats.Messages.Models.Block;

namespace Nethermind.SilStats.Test;

public class SilStatsIntegrationTests
{
    [TestCase(3UL, 1UL, 1UL, 3UL, TestName = "NormalizeHistoryRange_swaps_min_and_max")]
    [TestCase(0UL, 100UL, 37UL, 100UL, TestName = "NormalizeHistoryRange_limits_oversized_range")]
    [TestCase(ulong.MaxValue - 63, ulong.MaxValue, ulong.MaxValue - 63, ulong.MaxValue, TestName = "NormalizeHistoryRange_handles_max_ulong_without_overflow")]
    public void NormalizeHistoryRange_handles_edges(
        ulong requestMin,
        ulong requestMax,
        ulong expectedMin,
        ulong expectedMax)
    {
        SilStatsIntegration.NormalizeHistoryRange(
            new SilStatsHistoryRequest(requestMin, requestMax),
            out ulong min,
            out ulong max);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(min, Is.EqualTo(expectedMin));
            Assert.That(max, Is.EqualTo(expectedMax));
        }
    }

    [Test]
    public async Task HandleIncomingMessageAsync_sends_history_in_ascending_order()
    {
        IMessageSender sender = Substitute.For<IMessageSender>();
        IBlockTree blockTree = Substitute.For<IBlockTree>();
        SilStatsIntegration integration = CreateIntegration(sender, blockTree);
        CoreBlock firstBlock = Build.A.Block.WithNumber(1).TestObject;
        CoreBlock secondBlock = Build.A.Block.WithNumber(2).TestObject;

        blockTree.FindBlock(1UL, BlockTreeLookupOptions.RequireCanonical).Returns(firstBlock);
        blockTree.FindBlock(2UL, BlockTreeLookupOptions.RequireCanonical).Returns(secondBlock);
        sender.SendAsync(Arg.Any<IWebsocketClient>(), Arg.Any<HistoryMessage>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        await integration.HandleIncomingMessageAsync("""{"emit":["history",{"min":1,"max":2}]}""");

        _ = sender.Received(1).SendAsync(
            Arg.Any<IWebsocketClient>(),
            Arg.Is<HistoryMessage>(message => HasHistoryBlocks(message, 1, 2)),
            null);
    }

    private static SilStatsIntegration CreateIntegration(IMessageSender sender, IBlockTree blockTree)
        => new(
            "test-node",
            "test-client",
            30303,
            "testnet",
            "sil/68",
            "No",
            "Nethermind/v1",
            "contact",
            true,
            "secret",
            Substitute.For<ISilStatsClient>(),
            sender,
            Substitute.For<ITxPool>(),
            blockTree,
            Substitute.For<IPeerManager>(),
            Substitute.For<IGasPriceOracle>(),
            Substitute.For<ISilSyncingInfo>(),
            false,
            TimeSpan.FromSeconds(1),
            LimboLogs.Instance);

    private static bool HasHistoryBlocks(HistoryMessage message, ulong firstBlockNumber, ulong secondBlockNumber)
    {
        List<ulong> numbers = [];
        foreach (SilStatsBlock block in message.History)
        {
            numbers.Add(block.Number);
        }

        return numbers.Count == 2 &&
            numbers[0] == firstBlockNumber &&
            numbers[1] == secondBlockNumber;
    }
}
