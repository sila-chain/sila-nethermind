// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;

namespace Nethermind.Specs.Forks;

public class SilaPrague() : NamedReleaseSpec<SilaPrague>(SilaCancun.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "SilaPrague";
        spec.IsSip2537Enabled = true;
        spec.IsSip2935Enabled = true;
        spec.IsSip7702Enabled = true;
        spec.IsSip6110Enabled = true;
        spec.IsSip7002Enabled = true;
        spec.IsSip7251Enabled = true;
        spec.IsSip7623Enabled = true;
        spec.Sip2935ContractAddress = Sip2935Constants.BlockHashHistoryAddress;
        spec.MaxBlobCount = 9;
        spec.TargetBlobCount = 6;
        spec.BlobBaseFeeUpdateFraction = 5007716;
        spec.DepositContractAddress = Sip6110Constants.MainnetDepositContractAddress;
        spec.EngineApiNewPayloadVersion = EngineApiVersions.NewPayload.V4;
        spec.EngineApiGetPayloadVersion = EngineApiVersions.GetPayload.V4;
        // forkchoice version inherits V3 from SilaCancun
        // bodies versions inherit V1 from SilaShanghai
    }
}
