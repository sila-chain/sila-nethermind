// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Stats.Model
{
    /// <summary>
    /// Sil network level disconnect reason
    /// </summary>
    public enum SilDisconnectReason : byte
    {
        DisconnectRequested = 0x00,
        TcpSubSystemError = 0x01,
        BreachOfProtocol = 0x02,
        UselessPeer = 0x03,
        TooManyPeers = 0x04,
        AlreadyConnected = 0x05,
        IncompatibleP2PVersion = 0x06,
        NullNodeIdentityReceived = 0x07,
        ClientQuitting = 0x08,
        UnexpectedIdentity = 0x09,
        IdentitySameAsSelf = 0x0a,
        ReceiveMessageTimeout = 0x0b,
        MultipleHeaderDependencies = 0x0c,
        Other = 0x10
    }

    public static class SilDisconnectReasonExtensions
    {
        public static DisconnectReason ToDisconnectReason(this SilDisconnectReason reason) => reason switch
        {
            SilDisconnectReason.DisconnectRequested => DisconnectReason.SilDisconnectRequested,
            SilDisconnectReason.TcpSubSystemError => DisconnectReason.TcpSubSystemError,
            SilDisconnectReason.BreachOfProtocol => DisconnectReason.BreachOfProtocol,
            SilDisconnectReason.UselessPeer => DisconnectReason.UselessPeer,
            SilDisconnectReason.TooManyPeers => DisconnectReason.TooManyPeers,
            SilDisconnectReason.AlreadyConnected => DisconnectReason.AlreadyConnected,
            SilDisconnectReason.IncompatibleP2PVersion => DisconnectReason.IncompatibleP2PVersion,
            SilDisconnectReason.NullNodeIdentityReceived => DisconnectReason.NullNodeIdentityReceived,
            SilDisconnectReason.ClientQuitting => DisconnectReason.ClientQuitting,
            SilDisconnectReason.UnexpectedIdentity => DisconnectReason.UnexpectedIdentity,
            SilDisconnectReason.IdentitySameAsSelf => DisconnectReason.IdentitySameAsSelf,
            SilDisconnectReason.ReceiveMessageTimeout => DisconnectReason.ReceiveMessageTimeout,
            _ => DisconnectReason.Other,
        };
    }
}
