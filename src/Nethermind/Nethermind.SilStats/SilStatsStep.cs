// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Threading;
using System.Threading.Tasks;
using Nethermind.Api.Steps;
using Nethermind.Blockchain;
using Nethermind.Config;
using Nethermind.Consensus;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Specs;
using Nethermind.SilStats.Clients;
using Nethermind.SilStats.Integrations;
using Nethermind.SilStats.Senders;
using Nethermind.Facade.Sil;
using Nethermind.Init.Steps;
using Nethermind.JsonRpc.Modules.Sil.GasPrice;
using Nethermind.Logging;
using Nethermind.Network;
using Nethermind.Network.Config;
using Nethermind.Network.P2P;
using Nethermind.TxPool;

namespace Nethermind.SilStats;

[RunnerStepDependencies(typeof(InitializeBlockchain))]
public class SilStatsStep(
    ISpecProvider specProvider,
    ITxPool txPool,
    IBlockTree blockTree,
    IPeerManager peerManager,
    IGasPriceOracle gasPriceOracle,
    ISilSyncingInfo silSyncingInfo,
    IEnode enode,
    ISilStatsConfig silStatsConfig,
    INetworkConfig networkConfig,
    IMiningConfig miningConfig,
    ILogManager logManager
) : IStep, IAsyncDisposable
{
    private readonly ILogger _logger = logManager.GetClassLogger<SilStatsStep>();

    private ISilStatsIntegration _ethStatsIntegration = null!;
    public async Task Execute(CancellationToken cancellationToken)
    {
        string instanceId = $"{silStatsConfig.Name}-{Keccak.Compute(enode!.Info)}";
        if (_logger.IsInfo)
        {
            _logger.Info($"Initializing SIL Stats for the instance: {instanceId}, server: {silStatsConfig.Server}");
        }
        MessageSender sender = new(instanceId, logManager);
        const int reconnectionInterval = 5000;
        const string api = "no";
        const string client = "0.1.1";
        const bool canUpdateHistory = true;
        string node = ProductInfo.ClientId;
        int port = networkConfig.P2PPort;
        string network = specProvider!.NetworkId.ToString();
        string protocol = $"{P2PProtocolInfoProvider.DefaultCapabilitiesToString()}";

        ISilStatsClient silStatsClient = new SilStatsClient(
            silStatsConfig.Server,
            reconnectionInterval,
            sender,
            logManager);

        _ethStatsIntegration = new SilStatsIntegration(
            silStatsConfig.Name!,
            node,
            port,
            network,
            protocol,
            api,
            client,
            silStatsConfig.Contact!,
            canUpdateHistory,
            silStatsConfig.Secret!,
            silStatsClient,
            sender,
            txPool!,
            blockTree!,
            peerManager!,
            gasPriceOracle!,
            silSyncingInfo!,
            miningConfig.Enabled,
            TimeSpan.FromSeconds(silStatsConfig.SendInterval),
            logManager);

        await _ethStatsIntegration.InitAsync();
    }

    public ValueTask DisposeAsync()
    {
        _ethStatsIntegration.Dispose();
        return ValueTask.CompletedTask;
    }
}
