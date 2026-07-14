// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using Nethermind.Network;
using Nethermind.Network.Contract.P2P;
using Nethermind.Stats.Model;

namespace Nethermind.Xdc;

/// <summary>
/// XDC advertises sil/62, sil/63 and sil/100. The default sil/68 resolver is dropped at registration
/// (see <c>XdcModule</c>), so this resolver only contributes the XDC-specific versions.
/// </summary>
public class XdcP2PCapabilityResolver : IP2PCapabilityResolver
{
    // XDC's capability set is static, so the cache never needs invalidating.
    public event Action? Changed { add { } remove { } }

    public void Resolve(ISet<Capability> capabilities)
    {
        capabilities.Add(new Capability(Protocol.Sil, 62));
        capabilities.Add(new Capability(Protocol.Sil, 63));
        capabilities.Add(new Capability(Protocol.Sil, 100));
    }
}
