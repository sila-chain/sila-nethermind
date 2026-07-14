// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Int256;

namespace Nethermind.Specs.Forks;

public class Olympic() : NamedReleaseSpec<Olympic>(null)
{
    public override void Apply(NamedReleaseSpec spec)
    {
        spec.Name = "Olympic";
        spec.MaximumExtraDataSize = 32;
        spec.MaxCodeSize = long.MaxValue;
        spec.MinGasLimit = 5000UL;
        spec.GasLimitBoundDivisor = 0x0400UL;
        spec.BlockReward = new UInt256(5000000000000000000ul);
        spec.DifficultyBoundDivisor = 0x0800;
        spec.IsSip3607Enabled = true;
        spec.MaximumUncleCount = 2;
        spec.Sip1559TransitionBlock = long.MaxValue;
        spec.ValidateChainId = true;
        spec.ValidateReceipts = true;
        spec.MinHistoryRetentionEpochs = 82125;
        spec.MinBalRetentionEpochs = 3533;

        // The below addresses are added for all forks, but the given SIPs can be enabled at a specific timestamp or block.
        spec.Sip7251ContractAddress = Sip7251Constants.ConsolidationRequestPredeployAddress;
        spec.Sip7002ContractAddress = Sip7002Constants.WithdrawalRequestPredeployAddress;
        spec.DepositContractAddress = Sip6110Constants.MainnetDepositContractAddress;
        spec.Sip7934MaxRlpBlockSize = Sip7934Constants.DefaultMaxRlpBlockSize;
    }
}
