// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Nethermind.Core;
using Nethermind.Core.Precompiles;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Specs;

public class ReleaseSpec : IReleaseSpec
{
    public string Name { get; set; } = "Custom";
    public long MaximumExtraDataSize { get; set; }
    public long MaxCodeSize { get; set; }
    public ulong MinGasLimit { get; set; }
    public ulong MinHistoryRetentionEpochs { get; set; }
    public ulong MinBalRetentionEpochs { get; set; }
    public ulong GasLimitBoundDivisor { get; set; }
    public UInt256 BlockReward { get; set; }
    public ulong DifficultyBombDelay { get; set; }
    public ulong DifficultyBoundDivisor { get; set; }
    public ulong? FixedDifficulty { get; set; }
    public int MaximumUncleCount { get; set; }
    public bool IsTimeAdjustmentPostOlympic { get; set; }
    public bool IsSip2Enabled { get; set; }
    public bool IsSip7Enabled { get; set; }
    public bool IsSip100Enabled { get; set; }
    public bool IsSip140Enabled { get; set; }
    public bool IsSip150Enabled { get; set; }
    public bool IsSip155Enabled { get; set; }
    public bool IsSip158Enabled { get; set; }
    public bool IsSip160Enabled { get; set; }
    public bool IsSip170Enabled { get; set; }
    public bool IsSip196Enabled { get; set; }
    public bool IsSip197Enabled { get; set; }
    public bool IsSip198Enabled { get; set; }
    public bool IsSip211Enabled { get; set; }
    public bool IsSip214Enabled { get; set; }
    public bool IsSip649Enabled { get; set; }
    public bool IsSip658Enabled { get; set; }
    public bool IsSip145Enabled { get; set; }
    public bool IsSip1014Enabled { get; set; }
    public bool IsSip1052Enabled { get; set; }
    public bool IsSip1283Enabled { get; set; }
    public bool IsSip1234Enabled { get; set; }
    public bool IsSip1344Enabled { get; set; }
    public bool IsSip2028Enabled { get; set; }
    public bool IsSip152Enabled { get; set; }
    public bool IsSip1108Enabled { get; set; }
    public bool IsSip1884Enabled { get; set; }
    public bool IsSip2200Enabled { get; set; }
    public bool IsSip2537Enabled { get; set; }
    public bool IsSip2565Enabled { get; set; }
    public bool IsSip2929Enabled { get; set; }
    public bool IsSip2930Enabled { get; set; }
    public bool IsSip1559Enabled { get => field || IsSip4844Enabled; set; }
    public Address? Sip158IgnoredAccount { get; set; }
    public bool IsSip3198Enabled { get; set; }
    public bool IsSip3529Enabled { get; set; }
    public bool IsSip3607Enabled { get; set; }
    public bool IsSip3541Enabled { get; set; }
    public bool ValidateChainId { get; set; }
    public bool ValidateReceipts { get; set; }
    public ulong Sip1559TransitionBlock { get; set; }
    public ulong WithdrawalTimestamp { get; set; }
    public ulong Sip4844TransitionTimestamp { get; set; }
    public Address? FeeCollector { get; set; }
    public UInt256? Sip1559BaseFeeMinValue { get; set; }
    public UInt256 ForkBaseFee { get; set; } = Sip1559Constants.DefaultForkBaseFee;
    public UInt256 BaseFeeMaxChangeDenominator { get; set; } = Sip1559Constants.DefaultBaseFeeMaxChangeDenominator;
    public ulong ElasticityMultiplier { get; set; } = Sip1559Constants.DefaultElasticityMultiplier;
    public IBaseFeeCalculator BaseFeeCalculator { get; set; } = new DefaultBaseFeeCalculator();
    public bool IsSip1153Enabled { get; set; }
    public bool IsSip3651Enabled { get; set; }
    public bool IsSip3855Enabled { get; set; }
    public bool IsSip3860Enabled { get; set; }
    public bool IsSip4895Enabled { get; set; }
    public bool IsSip4844Enabled { get; set; }
    public bool IsSip7951Enabled { get; set; }
    public bool IsRip7212Enabled { get; set; }
    public bool IsSip7623Enabled { get; set; }
    public bool IsSip7976Enabled { get; set; }
    public bool IsSip7981Enabled { get; set; }
    public bool IsSip7883Enabled { get; set; }
    public bool IsSip5656Enabled { get; set; }
    public bool IsSip6780Enabled { get; set; }
    public bool IsSip8038Enabled { get; set; }
    public bool IsSip8282Enabled { get; set; }
    public bool IsSip4788Enabled { get; set; }
    public bool IsSip7702Enabled { get; set; }
    public bool IsSip7823Enabled { get; set; }
    public bool IsSip4844FeeCollectorEnabled { get; set; }
    public bool IsSip7002Enabled { get; set; }
    public bool IsSip7251Enabled { get; set; }
    public bool IsSip7825Enabled { get; set; }
    public bool IsSip7918Enabled { get; set; }
    public bool IsSip7934Enabled { get; set; }
    public int Sip7934MaxRlpBlockSize { get; set; }
    public ulong TargetBlobCount { get; set; }
    public ulong MaxBlobCount { get; set; }
    public ulong MaxBlobsPerTx => IsSip7594Enabled ? Math.Min(Sip7594Constants.MaxBlobsPerTx, MaxBlobCount) : MaxBlobCount;
    public ulong BlobBaseFeeUpdateFraction { get; set; }
    [MemberNotNullWhen(true, nameof(IsSip7251Enabled))]
    public Address? Sip7251ContractAddress { get => IsSip7251Enabled ? field : null; set; }
    [MemberNotNullWhen(true, nameof(Sip7002ContractAddress))]
    public Address? Sip7002ContractAddress { get => IsSip7002Enabled ? field : null; set; }
    [MemberNotNullWhen(true, nameof(IsSip4788Enabled))]
    public Address? Sip4788ContractAddress { get => IsSip4788Enabled ? field : null; set; }
    public bool IsSip8024Enabled { get; set; }
    public bool IsSip6110Enabled { get; set; }
    [MemberNotNullWhen(true, nameof(IsSip6110Enabled))]
    public Address? DepositContractAddress { get => IsSip6110Enabled ? field : null; set; }
    public bool IsSip2935Enabled { get; set; }
    public bool IsSip7709Enabled { get; set; }
    [MemberNotNullWhen(true, nameof(Sip2935ContractAddress))]
    public Address? Sip2935ContractAddress { get => IsSip2935Enabled ? field : null; set; }
    public bool IsSip7594Enabled { get; set; }
    public bool IsSip7939Enabled { get; set; }
    private FrozenSet<AddressAsKey>? _precompiles;
    FrozenSet<AddressAsKey> IReleaseSpec.Precompiles => _precompiles ??= BuildPrecompilesCache();
    private SpecGasCosts? _gasCosts;
    public SpecGasCosts GasCosts => _gasCosts ??= new SpecGasCosts(this);
    public ulong Sip2935RingBufferSize { get; set; } = Sip2935Constants.RingBufferSize;
    public virtual FrozenSet<AddressAsKey> BuildPrecompilesCache()
    {
        HashSet<AddressAsKey> cache =
        [
            PrecompiledAddresses.ECRecover,
            PrecompiledAddresses.Sha256,
            PrecompiledAddresses.Ripemd160,
            PrecompiledAddresses.Identity,
        ];

        if (IsSip198Enabled) cache.Add(PrecompiledAddresses.ModExp);
        if (IsSip196Enabled && IsSip197Enabled)
        {
            cache.Add(PrecompiledAddresses.BN254Add);
            cache.Add(PrecompiledAddresses.BN254Mul);
            cache.Add(PrecompiledAddresses.BN254PairingCheck);
        }

        if (IsSip152Enabled) cache.Add(PrecompiledAddresses.Blake2F);
        if (IsSip4844Enabled) cache.Add(PrecompiledAddresses.PointEvaluation);
        if (IsSip2537Enabled)
        {
            cache.Add(PrecompiledAddresses.Bls12381G1Add);
            cache.Add(PrecompiledAddresses.Bls12381G1Msm);
            cache.Add(PrecompiledAddresses.Bls12381G2Add);
            cache.Add(PrecompiledAddresses.Bls12381G2Msm);
            cache.Add(PrecompiledAddresses.Bls12381PairingCheck);
            cache.Add(PrecompiledAddresses.Bls12381FpToG1);
            cache.Add(PrecompiledAddresses.Bls12381Fp2ToG2);
        }

        if (IsRip7212Enabled || IsSip7951Enabled) cache.Add(PrecompiledAddresses.P256Verify);

        return cache.ToFrozenSet();
    }

    public bool IsSip7928Enabled { get; set; }
    public bool IsSip8037Enabled { get; set; }
    public bool IsSip7778Enabled { get; set; }
    public bool IsSip7843Enabled { get; set; }

    public bool IsSip7708Enabled { get; set; }
    public bool IsSip7954Enabled { get; set; }
    public bool IsSip8246Enabled { get; set; }
    public bool IsSip2780Enabled { get; set; }

    private ReleaseSpec? _systemSpec;

    internal ReleaseSpec SystemSpec => _systemSpec ??= CreateSystemSpec();

    private ReleaseSpec CreateSystemSpec()
    {
        ReleaseSpec clone = Clone();
        clone.IsSip158Enabled = false;
        return clone;
    }

    // used only in testing
    public ReleaseSpec Clone() => (ReleaseSpec)MemberwiseClone();
}
