// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Consensus;
using Nethermind.Consensus.Scheduler;
using Nethermind.Logging;
using Nethermind.Network.Contract.P2P;
using Nethermind.Network.P2P.ProtocolHandlers;
using Nethermind.Network.P2P.Subprotocols.Sil.V66;
using Nethermind.Network.Rlpx;
using Nethermind.Stats;
using Nethermind.Synchronization;
using Nethermind.TxPool;

namespace Nethermind.Network.P2P.Subprotocols.Sil.V67;

/// <summary>
/// https://github.com/sila-chain/SIPs/blob/master/SIPS/sip-4938.md
/// </summary>
public class Sil67ProtocolHandler(
    ISession session,
    IMessageSerializationService serializer,
    INodeStatsManager nodeStatsManager,
    ISyncServer syncServer,
    IBackgroundTaskScheduler backgroundTaskScheduler,
    ITxPool txPool,
    IGossipPolicy gossipPolicy,
    IForkInfo forkInfo,
    ILogManager logManager,
    ITxGossipPolicy? transactionsGossipPolicy = null)
    : Sil66ProtocolHandler(session, serializer, nodeStatsManager, syncServer, backgroundTaskScheduler, txPool,
        gossipPolicy, forkInfo, logManager, transactionsGossipPolicy), IStaticProtocolInfo
{
    public override string Name => "sil67";

    public new static byte Version => SilVersions.Sil67;
    public override byte ProtocolVersion => Version;

    protected override bool HandleMessageCore(ZeroPacket message)
    {
        switch (message.PacketType)
        {
            case Sil66MessageCode.GetNodeData:
            case Sil66MessageCode.NodeData:
                // sil/67 removed these codes — peers emitting them are breaching the protocol
                return false;
            default:
                return base.HandleMessageCore(message);
        }
    }
}
