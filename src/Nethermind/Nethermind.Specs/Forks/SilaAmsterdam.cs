// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Specs;

namespace Nethermind.Specs.Forks;

public class SilaAmsterdam() : NamedReleaseSpec<SilaAmsterdam>(BPO2.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "SilaAmsterdam";
        spec.IsSip2780Enabled = true;
        spec.IsSip7976Enabled = true;
        spec.IsSip7981Enabled = true;
        spec.IsSip7708Enabled = true;
        spec.IsSip7778Enabled = true;
        spec.IsSip7843Enabled = true;
        spec.IsSip7928Enabled = true;
        spec.IsSip7954Enabled = true;
        spec.MaxCodeSize = CodeSizeConstants.MaxCodeSizeSip7954;
        spec.IsSip8024Enabled = true;
        spec.IsSip8037Enabled = true;
        spec.IsSip8038Enabled = true;
        spec.IsSip8246Enabled = true;
        spec.IsSip8282Enabled = true;
        spec.EngineApiNewPayloadVersion = EngineApiVersions.NewPayload.V5;
        spec.EngineApiGetPayloadVersion = EngineApiVersions.GetPayload.V6;
        spec.EngineApiForkchoiceVersion = EngineApiVersions.Fcu.V4;
        spec.EngineApiPayloadBodiesByHashVersion = EngineApiVersions.PayloadBodiesByHash.V2;
        spec.EngineApiPayloadBodiesByRangeVersion = EngineApiVersions.PayloadBodiesByRange.V2;
    }

    public static IReleaseSpec NoSip8037Instance { get; } = new SilaAmsterdam { IsSip8037Enabled = false };
}
