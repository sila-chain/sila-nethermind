// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;

namespace Nethermind.Core.Specs;

/// <summary>
/// Precomputed gas cost and refund values derived from an <see cref="IReleaseSpec"/> instance.
/// Since specs are per-fork singletons, these values are computed once and cached on the spec,
/// avoiding repeated interface dispatch chains on every SAVM opcode execution.
/// </summary>
public sealed class SpecGasCosts : IEquatable<SpecGasCosts>
{
    private readonly int _hashCode;
    public readonly ulong SLoadCost;
    public readonly ulong BalanceCost;
    public readonly ulong ExtCodeCost;
    public readonly ulong ExtCodeHashCost;
    public readonly ulong CallCost;
    public readonly ulong ExpByteCost;
    public readonly ulong SStoreResetCost;
    public readonly ulong TxDataNonZeroMultiplier;
    public readonly ulong TotalCostFloorPerToken;

    public readonly ulong NetMeteredSStoreCost;
    public readonly ulong ClearReversalRefund;
    public readonly ulong SetReversalRefund;
    public readonly ulong SClearRefund;
    public readonly ulong DestroyRefund;

    public readonly ulong MaxBlobGasPerBlock;
    public readonly ulong MaxBlobGasPerTx;
    public readonly ulong TargetBlobGasPerBlock;

    public SpecGasCosts(IReleaseSpec spec)
    {
        bool hotCold = spec.UseHotAndColdStorage;  // SIP-2929
        bool largeDDos = spec.UseLargeStateDDosProtection;  // SIP-1884
        bool shanghaiDDos = spec.UseShanghaiDDosProtection;   // SIP-150
        bool netIstanbul = spec.UseIstanbulNetGasMetering;  // SIP-2200
        bool netConstantinople = spec.UseConstantinopleNetGasMetering;  // SIP-1283

        ulong clearReversalRefund = ClearReversalRefund =
            hotCold ? RefundOf.SResetReversedHotCold
            : netIstanbul ? RefundOf.SResetReversedSip2200
            : netConstantinople ? RefundOf.SResetReversedSip1283
            : GasCostOf.Free;

        ulong setReversalRefund = SetReversalRefund =
            hotCold ? RefundOf.SSetReversedHotCold
            : netIstanbul ? RefundOf.SSetReversedSip2200
            : netConstantinople ? RefundOf.SSetReversedSip1283
            : GasCostOf.Free;

        ulong sStoreResetCost = SStoreResetCost = hotCold
            ? GasCostOf.SReset - GasCostOf.ColdSLoad
            : GasCostOf.SReset;

        ulong netMeteredSStoreCost = NetMeteredSStoreCost =
            spec.IsSip8038Enabled ? GasCostOf.Free
            : hotCold ? GasCostOf.WarmStateRead
            : netIstanbul ? GasCostOf.SStoreNetMeteredSip2200
            : netConstantinople ? GasCostOf.SStoreNetMeteredSip1283
            : GasCostOf.Free;

        BalanceCost =
            hotCold ? GasCostOf.Free
            : largeDDos ? GasCostOf.BalanceSip1884
            : shanghaiDDos ? GasCostOf.BalanceSip150
            : GasCostOf.Balance;

        SLoadCost =
            hotCold ? GasCostOf.Free
            : largeDDos ? GasCostOf.SLoadSip1884
            : shanghaiDDos ? GasCostOf.SLoadSip150
            : GasCostOf.SLoad;

        ExtCodeHashCost =
            hotCold ? GasCostOf.Free
            : largeDDos ? GasCostOf.ExtCodeHashSip1884
            : GasCostOf.ExtCodeHash;

        ExtCodeCost =
            hotCold ? GasCostOf.Free
            : shanghaiDDos ? GasCostOf.ExtCodeSip150
            : GasCostOf.ExtCode;

        CallCost =
            hotCold ? GasCostOf.Free
            : shanghaiDDos ? GasCostOf.CallSip150
            : GasCostOf.Call;

        ExpByteCost = spec.UseExpDDosProtection
            ? GasCostOf.ExpByteSip160
            : GasCostOf.ExpByte;

        MaxBlobGasPerBlock = spec.MaxBlobCount * Sip4844Constants.GasPerBlob;
        MaxBlobGasPerTx = spec.MaxBlobsPerTx * Sip4844Constants.GasPerBlob;
        TargetBlobGasPerBlock = spec.TargetBlobCount * Sip4844Constants.GasPerBlob;

        TxDataNonZeroMultiplier = spec.IsSip2028Enabled
            ? GasCostOf.TxDataNonZeroMultiplierSip2028
            : GasCostOf.TxDataNonZeroMultiplier;

        TotalCostFloorPerToken = spec.IsSip7976Enabled
            ? GasCostOf.TotalCostFloorPerTokenSip7976
            : spec.IsSip7623Enabled
                ? GasCostOf.TotalCostFloorPerTokenSip7623
                : GasCostOf.Free;

        SClearRefund = spec.IsSip8038Enabled
            ? RefundOf.SClearSip8038
            : spec.IsSip3529Enabled
                ? RefundOf.SClearAfterSip3529
                : RefundOf.SClearBeforeSip3529;

        DestroyRefund = spec.IsSip3529Enabled
            ? RefundOf.DestroyAfterSip3529
            : RefundOf.DestroyBeforeSip3529;

        int hashCode1 = HashCode.Combine(SLoadCost, BalanceCost, ExtCodeCost, ExtCodeHashCost, CallCost, ExpByteCost, sStoreResetCost, netMeteredSStoreCost);
        int hashCode2 = HashCode.Combine(TxDataNonZeroMultiplier, TotalCostFloorPerToken, clearReversalRefund, setReversalRefund, SClearRefund, MaxBlobGasPerBlock, MaxBlobGasPerTx, TargetBlobGasPerBlock);
        _hashCode = HashCode.Combine(hashCode1, hashCode2, DestroyRefund);
    }

    public ulong RefundFromReversal(bool originalIsZero) => originalIsZero
        ? SetReversalRefund
        : ClearReversalRefund;

    public bool Equals(SpecGasCosts? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _hashCode == other._hashCode
            && SLoadCost == other.SLoadCost
            && BalanceCost == other.BalanceCost
            && ExtCodeCost == other.ExtCodeCost
            && ExtCodeHashCost == other.ExtCodeHashCost
            && CallCost == other.CallCost
            && ExpByteCost == other.ExpByteCost
            && SStoreResetCost == other.SStoreResetCost
            && TxDataNonZeroMultiplier == other.TxDataNonZeroMultiplier
            && TotalCostFloorPerToken == other.TotalCostFloorPerToken
            && NetMeteredSStoreCost == other.NetMeteredSStoreCost
            && ClearReversalRefund == other.ClearReversalRefund
            && SetReversalRefund == other.SetReversalRefund
            && SClearRefund == other.SClearRefund
            && DestroyRefund == other.DestroyRefund
            && MaxBlobGasPerBlock == other.MaxBlobGasPerBlock
            && MaxBlobGasPerTx == other.MaxBlobGasPerTx
            && TargetBlobGasPerBlock == other.TargetBlobGasPerBlock;
    }

    public override bool Equals(object? obj) => obj is SpecGasCosts other && Equals(other);

    public override int GetHashCode() => _hashCode;
}
