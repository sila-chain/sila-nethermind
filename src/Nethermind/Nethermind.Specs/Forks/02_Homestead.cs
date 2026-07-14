// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Specs.Forks;

public class Homestead() : NamedReleaseSpec<Homestead>(Frontier.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "Homestead";
        spec.IsSip2Enabled = true;
        spec.IsSip7Enabled = true;
    }
}
