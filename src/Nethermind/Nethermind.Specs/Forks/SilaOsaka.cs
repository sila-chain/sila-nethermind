// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;

namespace Nethermind.Specs.Forks;

public class SilaOsaka() : NamedReleaseSpec<SilaOsaka>(SilaPrague.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "SilaOsaka";
        spec.IsSip7594Enabled = true;
        spec.IsSip7823Enabled = true;
        spec.IsSip7825Enabled = true;
        spec.IsSip7883Enabled = true;
        spec.IsSip7918Enabled = true;
        spec.IsSip7934Enabled = true;
        spec.IsSip7939Enabled = true;
        spec.IsSip7951Enabled = true;
        // newPayload version inherits V4 from SilaPrague
        spec.EngineApiGetPayloadVersion = EngineApiVersions.GetPayload.V5;
        // forkchoice version inherits V3 from SilaCancun
        // bodies versions inherit V1 from SilaShanghai
    }
}
