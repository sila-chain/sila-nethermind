// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nethermind.Core.Metric;
using Nethermind.Network.P2P.Subprotocols.Sil.V62;
using Nethermind.Network.P2P.Subprotocols.Sil.V63;
using Nethermind.Network.P2P.Subprotocols.Sil.V65;
using Nethermind.Network.P2P.Subprotocols.Sil.V66;
using Nethermind.Network.P2P.Subprotocols.Sil.V68;
using Nethermind.Network.P2P.Subprotocols.Sil.V69;
using Nethermind.Network.P2P.Subprotocols.Sil.V70;
using Nethermind.Network.P2P.Subprotocols.Snap;

namespace Nethermind.Network.P2P;

public readonly record struct VersionedProtocol(string Protocol, byte Version);

public readonly record struct P2PMessageKey(VersionedProtocol Protocol, int PacketType) : IMetricLabels
{
    private static readonly FrozenDictionary<(string, int), string> MessageNames =
        FromMessageCodeClass(Contract.P2P.Protocol.P2P, typeof(P2PMessageCode))
            .Concat(FromMessageCodeClass(Contract.P2P.Protocol.Sil, typeof(Sil62MessageCode)))
            .Concat(FromMessageCodeClass(Contract.P2P.Protocol.Sil, typeof(Sil63MessageCode)))
            .Concat(FromMessageCodeClass(Contract.P2P.Protocol.Sil, typeof(Sil65MessageCode)))
            .Concat(FromMessageCodeClass(Contract.P2P.Protocol.Sil, typeof(Sil66MessageCode)))
            .Concat(FromMessageCodeClass(Contract.P2P.Protocol.Sil, typeof(Sil68MessageCode)))
            .Concat(FromMessageCodeClass(Contract.P2P.Protocol.Sil, typeof(Sil69MessageCode)))
            .Concat(FromMessageCodeClass(Contract.P2P.Protocol.Sil, typeof(Sil70MessageCode)))
            .Concat(FromMessageCodeClass(Contract.P2P.Protocol.Snap, typeof(SnapMessageCode)))
            .ToFrozenDictionary();

    private static IEnumerable<KeyValuePair<(string, int), string>> FromMessageCodeClass(string protocol, Type classType) =>
        classType.GetFields(
                BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType.IsAssignableTo(typeof(int)))
            .Select(field => KeyValuePair.Create((protocol, (int)field.GetValue(null)), field.Name));

    private static readonly ConcurrentDictionary<P2PMessageKey, string[]> s_labelCache = new();
    public readonly string[] Labels => s_labelCache.GetOrAdd(this, static key => key.CalculateLabel());

    private readonly string[] CalculateLabel() => [$"{Protocol.Protocol}{Protocol.Version}", GetMessageType()];

    private readonly string GetMessageType()
    {
        if (!MessageNames.TryGetValue((Protocol.Protocol, PacketType), out string messageName))
        {
#if DEBUG
            throw new NotImplementedException($"Message name for protocol {Protocol.Protocol} message id {PacketType} not set.");
#else
            return PacketType.ToString(); // Just use the integer directly then
#endif
        }
        return messageName;
    }

    public override string ToString() => string.Join(',', Labels);
}
