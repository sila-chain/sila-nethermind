// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Stats.Model;

/// <summary>
/// Nethermind level disconnect reason. This is different than `SilDisconnectReason` as its more detailed, specific
/// to Nethermind and does not map 1-1 with sila disconnect message code. Useful for metrics, debugging and
/// peer disconnect delays.
/// Don't forget to add the corresponding Sil level disconnect reason in `DisconnectReasonExtension`.
/// </summary>
public enum DisconnectReason : byte
{
    // Connection related
    SessionIdAlreadyExists,
    ConnectionClosed,
    OutgoingConnectionFailed,
    DuplicatedConnection,
    PeerRemoved,
    TooManyPeers,
    HardLimitTooManyPeers,
    SessionAlreadyExist,
    ReplacingSessionWithOppositeDirection,
    OppositeDirectionCleanup,
    BackgroundTaskFailure,
    ConnectionReset,
    Exception,

    // Non sync, non connection related disconnect
    SnapServerNotImplemented,
    IncompatibleP2PVersion,
    InvalidNetworkId,
    InvalidGenesis,
    MissingForkId,
    InvalidForkId,
    ProtocolInitTimeout,
    TxFlooding,
    InvalidTxReceived,
    NoCapabilityMatched,
    ClientFiltered,
    AppClosing,
    DropWorstPeer,
    PeerRefreshFailed,
    GossipingInPoS,

    // Sync related
    ForwardSyncFailed,
    InvalidTxOrUncle,
    HeaderResponseTooLong,
    InconsistentHeaderBatch,
    UnexpectedHeaderHash,
    HeaderBatchOnDifferentBranch,
    UnexpectedParentHeader,
    InvalidHeader,
    InvalidReceiptRoot,
    SilSyncException,
    InvalidBlockRangeUpdate,
    MessageLimitsBreached,

    // These are from SilDisconnectReason that does not necessarily use in Nethermind.
    SilDisconnectRequested,
    TcpSubSystemError,
    BreachOfProtocol,
    UselessPeer,
    AlreadyConnected,
    NullNodeIdentityReceived,
    ClientQuitting,
    UnexpectedIdentity,
    IdentitySameAsSelf,
    ReceiveMessageTimeout,
    MultipleHeaderDependencies,

    // Try not to use this. Instead, create a new one.
    Other,
}

public static class DisconnectReasonExtension
{
    public static SilDisconnectReason ToEthDisconnectReason(this DisconnectReason disconnectReason) => disconnectReason switch
    {
        DisconnectReason.TooManyPeers => SilDisconnectReason.TooManyPeers,
        DisconnectReason.HardLimitTooManyPeers => SilDisconnectReason.TooManyPeers,
        DisconnectReason.SessionAlreadyExist or DisconnectReason.ReplacingSessionWithOppositeDirection or DisconnectReason.OppositeDirectionCleanup or DisconnectReason.DuplicatedConnection or DisconnectReason.SessionIdAlreadyExists => SilDisconnectReason.AlreadyConnected,
        DisconnectReason.ConnectionClosed or DisconnectReason.OutgoingConnectionFailed => SilDisconnectReason.TcpSubSystemError,
        DisconnectReason.IncompatibleP2PVersion => SilDisconnectReason.IncompatibleP2PVersion,
        // Chain-identity mismatches are protocol breaches so that even static nodes are disconnected from a wrong network.
        DisconnectReason.InvalidNetworkId or DisconnectReason.InvalidGenesis or DisconnectReason.MissingForkId or DisconnectReason.InvalidForkId => SilDisconnectReason.BreachOfProtocol,
        DisconnectReason.ClientFiltered => SilDisconnectReason.DisconnectRequested,
        DisconnectReason.ProtocolInitTimeout => SilDisconnectReason.ReceiveMessageTimeout,
        DisconnectReason.SnapServerNotImplemented or DisconnectReason.TxFlooding or DisconnectReason.NoCapabilityMatched => SilDisconnectReason.UselessPeer,
        DisconnectReason.DropWorstPeer => SilDisconnectReason.TooManyPeers,
        DisconnectReason.PeerRemoved or DisconnectReason.PeerRefreshFailed => SilDisconnectReason.DisconnectRequested,
        DisconnectReason.ForwardSyncFailed => SilDisconnectReason.DisconnectRequested,
        DisconnectReason.GossipingInPoS => SilDisconnectReason.BreachOfProtocol,
        DisconnectReason.AppClosing => SilDisconnectReason.ClientQuitting,
        DisconnectReason.InvalidTxOrUncle or DisconnectReason.HeaderResponseTooLong or DisconnectReason.InconsistentHeaderBatch or DisconnectReason.UnexpectedHeaderHash or DisconnectReason.HeaderBatchOnDifferentBranch or DisconnectReason.UnexpectedParentHeader or DisconnectReason.InvalidHeader or DisconnectReason.InvalidReceiptRoot or DisconnectReason.SilSyncException or DisconnectReason.InvalidBlockRangeUpdate => SilDisconnectReason.BreachOfProtocol,
        DisconnectReason.SilDisconnectRequested => SilDisconnectReason.DisconnectRequested,
        DisconnectReason.TcpSubSystemError => SilDisconnectReason.TcpSubSystemError,
        DisconnectReason.BreachOfProtocol => SilDisconnectReason.BreachOfProtocol,
        DisconnectReason.MessageLimitsBreached => SilDisconnectReason.BreachOfProtocol,
        DisconnectReason.UselessPeer => SilDisconnectReason.UselessPeer,
        DisconnectReason.AlreadyConnected => SilDisconnectReason.AlreadyConnected,
        DisconnectReason.NullNodeIdentityReceived => SilDisconnectReason.NullNodeIdentityReceived,
        DisconnectReason.ClientQuitting => SilDisconnectReason.ClientQuitting,
        DisconnectReason.UnexpectedIdentity => SilDisconnectReason.UnexpectedIdentity,
        DisconnectReason.IdentitySameAsSelf => SilDisconnectReason.IdentitySameAsSelf,
        DisconnectReason.ReceiveMessageTimeout => SilDisconnectReason.ReceiveMessageTimeout,
        DisconnectReason.MultipleHeaderDependencies => SilDisconnectReason.MultipleHeaderDependencies,
        DisconnectReason.ConnectionReset => SilDisconnectReason.TcpSubSystemError,
        DisconnectReason.InvalidTxReceived => SilDisconnectReason.Other,
        _ => SilDisconnectReason.Other,
    };
}
