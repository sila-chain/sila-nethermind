// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Specs.Forks;

namespace Nethermind.Specs.GnosisForks;

public class PragueGnosis() : NamedGnosisReleaseSpec<PragueGnosis>(SilaPrague.Instance, CancunGnosis.Instance)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        base.Apply(spec);
        spec.IsSip4844FeeCollectorEnabled = true;
        spec.BlobBaseFeeUpdateFraction = 0x10fafa;
        spec.TargetBlobCount = 1;
        spec.MaxBlobCount = 2;
    }
}
