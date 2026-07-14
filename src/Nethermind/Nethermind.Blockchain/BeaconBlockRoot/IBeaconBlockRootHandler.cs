// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Sip2930;
using Nethermind.Core.Specs;
using Nethermind.Savm.Tracing;

namespace Nethermind.Blockchain.BeaconBlockRoot;

public interface IBeaconBlockRootHandler : IHasAccessList
{
    (Address? toAddress, AccessList? accessList) BeaconRootsAccessList(Block block, IReleaseSpec spec, bool includeStorageCells = true);
    void StoreBeaconRoot(Block block, IReleaseSpec spec, ITxTracer tracer);
}
