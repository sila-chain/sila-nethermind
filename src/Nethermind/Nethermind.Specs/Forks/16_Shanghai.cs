// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;

namespace Nethermind.Specs.Forks;

public class SilaShanghai() : NamedReleaseSpec<SilaShanghai>(SilaParis.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "SilaShanghai";
        spec.IsSip3651Enabled = true;
        spec.IsSip3855Enabled = true;
        spec.IsSip3860Enabled = true;
        spec.IsSip4895Enabled = true;
        spec.WithdrawalTimestamp = MainnetSpecProvider.ShanghaiBlockTimestamp;
        // SIP-3675: uncles are forbidden from the merge onwards. Pinned here (not SilaParis) because
        // MainnetSpecProvider maps the terminal PoW block to SilaParis.Instance, so spec-gating at
        // SilaParis would reject a consensus-valid pre-merge block. SilaShanghai is unambiguously post-merge.
        spec.MaximumUncleCount = 0;
        spec.EngineApiNewPayloadVersion = EngineApiVersions.NewPayload.V2;
        spec.EngineApiGetPayloadVersion = EngineApiVersions.GetPayload.V2;
        spec.EngineApiForkchoiceVersion = EngineApiVersions.Fcu.V2;
        spec.EngineApiPayloadBodiesByHashVersion = EngineApiVersions.PayloadBodiesByHash.V1;
        spec.EngineApiPayloadBodiesByRangeVersion = EngineApiVersions.PayloadBodiesByRange.V1;
    }
}
