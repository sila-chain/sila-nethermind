// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Specs.Forks;

public class Istanbul() : NamedReleaseSpec<Istanbul>(ConstantinopleFix.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "Istanbul";
        spec.IsSip1344Enabled = true;
        spec.IsSip2028Enabled = true;
        spec.IsSip152Enabled = true;
        spec.IsSip1108Enabled = true;
        spec.IsSip1884Enabled = true;
        spec.IsSip2200Enabled = true;
    }
}
