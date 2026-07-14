// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Int256;

namespace Nethermind.Specs.Forks;

public class Byzantium() : NamedReleaseSpec<Byzantium>(SpuriousDragon.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "Byzantium";
        spec.BlockReward = new UInt256(3000000000000000000ul);
        spec.DifficultyBombDelay = 3000000L;
        spec.IsSip100Enabled = true;
        spec.IsSip140Enabled = true;
        spec.IsSip196Enabled = true;
        spec.IsSip197Enabled = true;
        spec.IsSip198Enabled = true;
        spec.IsSip211Enabled = true;
        spec.IsSip214Enabled = true;
        spec.IsSip649Enabled = true;
        spec.IsSip658Enabled = true;
    }
}
