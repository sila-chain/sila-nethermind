// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Specs.Forks;
using Nethermind.Specs.GnosisForks;

namespace Sila.Test.Base;

public static class ChainUtils
{
    public static IReleaseSpec? ResolveSpec(IReleaseSpec? spec, ulong chainId) =>
        chainId == BlockchainIds.Gnosis
            ? spec switch
            {
                SilaAmsterdam => AmsterdamGnosis.Instance,
                SilaPrague => PragueGnosis.Instance,
                SilaCancun => CancunGnosis.Instance,
                SilaShanghai => ShanghaiGnosis.Instance,
                London => LondonGnosis.Instance,
                _ => spec
            }
            : spec;
}
