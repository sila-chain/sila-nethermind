// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Savm;
using Nethermind.Savm.State;

namespace Nethermind.Blockchain;

// Mainly used by tests
public class SilaCodeInfoRepository(IWorldState worldState) : CacheCodeInfoRepository(worldState, new SilaPrecompileProvider(), StaticCodeCache.Instance)
{
}
