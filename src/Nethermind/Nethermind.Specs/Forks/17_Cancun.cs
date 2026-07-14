// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;

namespace Nethermind.Specs.Forks;

public class SilaCancun() : NamedReleaseSpec<SilaCancun>(SilaShanghai.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "SilaCancun";
        spec.IsSip1153Enabled = true;
        spec.IsSip4788Enabled = true;
        spec.IsSip4844Enabled = true;
        spec.IsSip5656Enabled = true;
        spec.IsSip6780Enabled = true;
        spec.Sip4788ContractAddress = Sip4788Constants.BeaconRootsAddress;
        spec.Sip4844TransitionTimestamp = MainnetSpecProvider.CancunBlockTimestamp;
        spec.MaxBlobCount = 6;
        spec.TargetBlobCount = 3;
        spec.BlobBaseFeeUpdateFraction = 3338477;
        spec.EngineApiNewPayloadVersion = EngineApiVersions.NewPayload.V3;
        spec.EngineApiGetPayloadVersion = EngineApiVersions.GetPayload.V3;
        spec.EngineApiForkchoiceVersion = EngineApiVersions.Fcu.V3;
        // bodies/hash + bodies/range versions inherit V1 from SilaShanghai
    }
}
