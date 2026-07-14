// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;

namespace Nethermind.Specs.Forks;

public class SpuriousDragon() : NamedReleaseSpec<SpuriousDragon>(TangerineWhistle.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "Spurious Dragon";
        spec.MaxCodeSize = CodeSizeConstants.MaxCodeSizeSip170;
        spec.IsSip155Enabled = true;
        spec.IsSip158Enabled = true;
        spec.IsSip160Enabled = true;
        spec.IsSip170Enabled = true;
    }
}
