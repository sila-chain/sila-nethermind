// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Text.RegularExpressions;
using Nethermind.Blockchain.Synchronization;
using Nethermind.Network.Contract.P2P;
using Nethermind.Stats.Model;

namespace Nethermind.Synchronization.Peers
{
    public static partial class SyncPeerExtensions
    {
        // Check if OpenSila supports GetNodeData
        private static readonly Version _openSilaSecondRemoveGetNodeDataVersion = new(3, 3, 3);
        private static readonly Version _openSilaFirstRemoveGetNodeDataVersion = new(3, 1, 0);

        public static bool SupportsAllocation(this PeerInfo peerInfo, AllocationContexts contexts)
        {
            if (contexts == AllocationContexts.BlockAccessLists && !peerInfo.SyncPeer.SupportsBlockAccessLists())
            {
                return false;
            }

            // check if OpenSila supports state sync
            if ((contexts & AllocationContexts.State) != 0 // only for State allocations
                && peerInfo.SyncPeer.ClientType == NodeClientType.OpenSila) // only for OE
            {
                // try get OpenSila version
                Version? openSilaVersion = peerInfo.SyncPeer.GetOpenSilaVersion(out _);
                if (openSilaVersion is not null)
                {
                    int versionComparison = openSilaVersion.CompareTo(_openSilaSecondRemoveGetNodeDataVersion);
                    return versionComparison >= 0 || openSilaVersion < _openSilaFirstRemoveGetNodeDataVersion;
                }
            }

            return true;
        }

        public static bool SupportsBlockAccessLists(this ISyncPeer peer) => peer.ProtocolVersion >= SilVersions.Sil71;

        private static readonly Regex _openSilaVersionRegex = OpenSilaRegex();

        public static Version? GetOpenSilaVersion(this ISyncPeer peer, out int releaseCandidate)
        {
            if (peer.ClientType == NodeClientType.OpenSila)
            {
                Match match = _openSilaVersionRegex.Match(peer.ClientId);

                if (match.Success && Version.TryParse(match.Groups["mainVersion"].Value, out Version version))
                {
                    int.TryParse(match.Groups["rc"].Value, out releaseCandidate);
                    return version;
                }
            }

            releaseCandidate = 0;
            return null;
        }

        [GeneratedRegex("OpenSila\\/([a-zA-z-0-9]*\\/)*v(?<version>(?<mainVersion>[0-9]\\.[0-9]\\.[0-9])-?(rc\\.(?<rc>[0-9]*))?)")]
        private static partial Regex OpenSilaRegex();
    }
}
