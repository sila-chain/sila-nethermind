// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Frozen;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Specs.Test
{
    /// <summary>
    /// Only for testing purposes.
    /// If we want to override only a few properties for tests based on different releases spec we can use this class.
    /// </summary>
    public class OverridableReleaseSpec(IReleaseSpec spec) : IReleaseSpec
    {
        public string Name => spec.Name;
        public long MaximumExtraDataSize { get; set; } = spec.MaximumExtraDataSize;
        public long MaxCodeSize { get; set; } = spec.MaxCodeSize;
        public ulong MinGasLimit { get; set; } = spec.MinGasLimit;
        public ulong MinHistoryRetentionEpochs { get; set; } = spec.MinHistoryRetentionEpochs;
        public ulong MinBalRetentionEpochs { get; set; } = spec.MinBalRetentionEpochs;
        public ulong GasLimitBoundDivisor { get; set; } = spec.GasLimitBoundDivisor;
        public UInt256 BlockReward { get; set; } = spec.BlockReward;
        public ulong DifficultyBombDelay { get; set; } = spec.DifficultyBombDelay;
        public ulong DifficultyBoundDivisor { get; set; } = spec.DifficultyBoundDivisor;
        public ulong? FixedDifficulty { get; set; } = spec.FixedDifficulty;
        public int MaximumUncleCount { get; set; } = spec.MaximumUncleCount;
        public bool IsTimeAdjustmentPostOlympic { get; set; } = spec.IsTimeAdjustmentPostOlympic;
        public bool IsSip2Enabled { get; set; } = spec.IsSip2Enabled;
        public bool IsSip7Enabled { get; set; } = spec.IsSip7Enabled;
        public bool IsSip100Enabled { get; set; } = spec.IsSip100Enabled;
        public bool IsSip140Enabled { get; set; } = spec.IsSip140Enabled;
        public bool IsSip150Enabled { get; set; } = spec.IsSip150Enabled;
        public bool IsSip155Enabled { get; set; } = spec.IsSip155Enabled;
        public bool IsSip158Enabled { get; set; } = spec.IsSip158Enabled;
        public bool IsSip160Enabled { get; set; } = spec.IsSip160Enabled;
        public bool IsSip170Enabled { get; set; } = spec.IsSip170Enabled;
        public bool IsSip196Enabled { get; set; } = spec.IsSip196Enabled;
        public bool IsSip197Enabled { get; set; } = spec.IsSip197Enabled;
        public bool IsSip198Enabled { get; set; } = spec.IsSip198Enabled;
        public bool IsSip211Enabled { get; set; } = spec.IsSip211Enabled;
        public bool IsSip214Enabled { get; set; } = spec.IsSip214Enabled;
        public bool IsSip649Enabled { get; set; } = spec.IsSip649Enabled;
        public bool IsSip658Enabled { get; set; } = spec.IsSip658Enabled;
        public bool IsSip145Enabled { get; set; } = spec.IsSip145Enabled;
        public bool IsSip1014Enabled { get; set; } = spec.IsSip1014Enabled;
        public bool IsSip1052Enabled { get; set; } = spec.IsSip1052Enabled;
        public bool IsSip1283Enabled { get; set; } = spec.IsSip1283Enabled;
        public bool IsSip1234Enabled { get; set; } = spec.IsSip1234Enabled;
        public bool IsSip1344Enabled { get; set; } = spec.IsSip1344Enabled;
        public bool IsSip2028Enabled { get; set; } = spec.IsSip2028Enabled;
        public bool IsSip152Enabled { get; set; } = spec.IsSip152Enabled;
        public bool IsSip1108Enabled { get; set; } = spec.IsSip1108Enabled;
        public bool IsSip1884Enabled { get; set; } = spec.IsSip1884Enabled;
        public bool IsSip2200Enabled { get; set; } = spec.IsSip2200Enabled;
        public bool IsSip2537Enabled { get; set; } = spec.IsSip2537Enabled;
        public bool IsSip2565Enabled { get; set; } = spec.IsSip2565Enabled;
        public bool IsSip2929Enabled { get; set; } = spec.IsSip2929Enabled;
        public bool IsSip2930Enabled { get; set; } = spec.IsSip2930Enabled;
        public bool IsSip1559Enabled { get; set; } = spec.IsSip1559Enabled;
        public bool IsSip3198Enabled { get; set; } = spec.IsSip3198Enabled;
        public bool IsSip3529Enabled { get; set; } = spec.IsSip3529Enabled;
        public bool IsSip3541Enabled { get; set; } = spec.IsSip3541Enabled;
        public bool IsSip4844Enabled { get; set; } = spec.IsSip4844Enabled;
        public bool IsSip7951Enabled { get; set; } = spec.IsSip7951Enabled;
        public bool IsRip7212Enabled { get; set; } = spec.IsRip7212Enabled;
        public bool IsSip7623Enabled { get; set; } = spec.IsSip7623Enabled;
        public bool IsSip7976Enabled { get; set; } = spec.IsSip7976Enabled;
        public bool IsSip7981Enabled { get; set; } = spec.IsSip7981Enabled;
        public bool IsSip7918Enabled { get; set; } = spec.IsSip7918Enabled;
        public bool IsSip7883Enabled { get; set; } = spec.IsSip7883Enabled;
        public bool IsSip7934Enabled { get; set; } = spec.IsSip7934Enabled;
        public int Sip7934MaxRlpBlockSize { get; set; } = spec.Sip7934MaxRlpBlockSize;
        public bool ValidateChainId { get; set; } = spec.ValidateChainId;
        public bool ValidateReceipts { get; set; } = spec.ValidateReceipts;
        public UInt256? Sip1559BaseFeeMinValue { get; set; } = spec.Sip1559BaseFeeMinValue;
        public bool IsSip3607Enabled { get; set; } = spec.IsSip3607Enabled;
        public Address? Sip158IgnoredAccount { get; set; } = spec.Sip158IgnoredAccount;
        public ulong Sip1559TransitionBlock { get; set; } = spec.Sip1559TransitionBlock;
        public Address? FeeCollector { get; set; } = spec.FeeCollector;
        public ulong Sip4844TransitionTimestamp { get; set; } = spec.Sip4844TransitionTimestamp;
        public ulong TargetBlobCount { get; set; } = spec.TargetBlobCount;
        public ulong MaxBlobCount { get; set; } = spec.MaxBlobCount;
        public ulong MaxBlobsPerTx { get; set; } = spec.MaxBlobsPerTx;
        public ulong BlobBaseFeeUpdateFraction { get; set; } = spec.BlobBaseFeeUpdateFraction;
        public bool IsSip1153Enabled { get; set; } = spec.IsSip1153Enabled;
        public bool IsSip3651Enabled { get; set; } = spec.IsSip3651Enabled;
        public bool IsSip3855Enabled { get; set; } = spec.IsSip3855Enabled;
        public bool IsSip3860Enabled { get; set; } = spec.IsSip3860Enabled;
        public bool IsSip4895Enabled { get; set; } = spec.IsSip4895Enabled;
        public ulong WithdrawalTimestamp { get; set; } = spec.WithdrawalTimestamp;
        public bool IsSip5656Enabled { get; set; } = spec.IsSip5656Enabled;
        public ulong Sip2935RingBufferSize { get; set; } = spec.Sip2935RingBufferSize;
        public bool IsSip6780Enabled { get; set; } = spec.IsSip6780Enabled;
        public bool IsSip8038Enabled { get; set; } = spec.IsSip8038Enabled;
        public bool IsSip8282Enabled { get; set; } = spec.IsSip8282Enabled;
        public bool IsSip4788Enabled { get; set; } = spec.IsSip4788Enabled;
        public bool IsSip4844FeeCollectorEnabled { get; set; } = spec.IsSip4844FeeCollectorEnabled;
        public Address? Sip4788ContractAddress { get; set; } = spec.Sip4788ContractAddress;
        public bool IsSip7002Enabled { get; set; } = spec.IsSip7002Enabled;
        public Address? Sip7002ContractAddress { get; set; } = spec.Sip7002ContractAddress;
        public bool IsSip7251Enabled { get; set; } = spec.IsSip7251Enabled;
        public Address? Sip7251ContractAddress { get; set; } = spec.Sip7251ContractAddress;
        public bool IsSip2935Enabled { get; set; } = spec.IsSip2935Enabled;
        public bool IsSip7709Enabled { get; set; } = spec.IsSip7709Enabled;
        public Address? Sip2935ContractAddress { get; set; } = spec.Sip2935ContractAddress;
        public bool IsSip7702Enabled { get; set; } = spec.IsSip7702Enabled;
        public bool IsSip7823Enabled { get; set; } = spec.IsSip7823Enabled;
        public bool IsSip7825Enabled { get; set; } = spec.IsSip7825Enabled;
        public UInt256 ForkBaseFee { get; set; } = spec.ForkBaseFee;
        public UInt256 BaseFeeMaxChangeDenominator { get; set; } = spec.BaseFeeMaxChangeDenominator;
        public ulong ElasticityMultiplier { get; set; } = spec.ElasticityMultiplier;
        public IBaseFeeCalculator BaseFeeCalculator { get; set; } = spec.BaseFeeCalculator;
        public bool IsSip8024Enabled { get; set; } = spec.IsSip8024Enabled;
        public bool IsSip6110Enabled { get; set; } = spec.IsSip6110Enabled;
        public Address? DepositContractAddress { get; set; } = spec.DepositContractAddress;
        public bool IsSip7594Enabled { get; set; } = spec.IsSip7594Enabled;
        public bool IsSip7939Enabled { get; set; } = spec.IsSip7939Enabled;
        public bool IsSip7928Enabled { get; set; } = spec.IsSip7928Enabled;
        public bool IsSip8037Enabled { get; set; } = spec.IsSip8037Enabled;
        public bool IsSip7708Enabled { get; set; } = spec.IsSip7708Enabled;
        public bool IsSip7778Enabled { get; set; } = spec.IsSip7778Enabled;
        public bool IsSip7843Enabled => spec.IsSip7843Enabled;
        public bool IsSip7954Enabled { get; set; } = spec.IsSip7954Enabled;
        public bool IsSip8246Enabled { get; set; } = spec.IsSip8246Enabled;
        public bool IsSip2780Enabled { get; set; } = spec.IsSip2780Enabled;
        public SpecGasCosts GasCosts => new(this);
        FrozenSet<AddressAsKey> IReleaseSpec.Precompiles => spec.Precompiles;
    }
}
