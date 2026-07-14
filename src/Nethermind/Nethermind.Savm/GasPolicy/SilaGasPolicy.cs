// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Savm.GasPolicy;

/// <summary>
/// Unified Sila gas policy supporting both legacy single-dimensional behavior and SIP-8037
/// two-dimensional behavior when opcode dispatch and spec flags enable it.
/// </summary>
/// <remarks>
/// The spill split fields below follow SIP-8037 block-gas accounting.
/// </remarks>
public struct SilaGasPolicy : IGasPolicy<SilaGasPolicy>
{
    /// <summary>Regular gas budget (legacy gas_left).</summary>
    public ulong Value;
    /// <summary>State gas reservoir used by SIP-8037 paths.</summary>
    public long StateReservoir;
    /// <summary>Cumulative state gas used for block accounting.</summary>
    public long StateGasUsed;
    /// <summary>State gas that spilled from gas_left (for block regular gas exclusion).</summary>
    public long StateGasSpill;
    /// <summary>Spill consumed by state refunds and excluded from block regular gas.</summary>
    public long StateGasSpillRefunded;
    /// <summary>Indicates that execution encountered an out of gas condition.</summary>
    public bool OutOfGas;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SilaGasPolicy FromULong(ulong value) => new() { Value = value };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SilaGasPolicy CreateSystemTransactionIntrinsicGas(ulong blockGasLimit) =>
        new()
        {
            Value = 0,
            StateReservoir = Sip8037Constants.SystemCallStateReservoir,
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SilaGasPolicy CreateSystemTransactionAvailableGas(ulong gasLimit, in SilaGasPolicy intrinsicGas, IReleaseSpec spec)
    {
        if (spec.IsSip8037Enabled)
        {
            long reservoir = Math.Min((long)gasLimit, intrinsicGas.StateReservoir);
            return new SilaGasPolicy
            {
                Value = gasLimit - (ulong)reservoir,
                StateReservoir = reservoir,
                StateGasUsed = intrinsicGas.StateGasUsed,
                StateGasSpill = 0,
            };
        }

        return CreateAvailableFromIntrinsic(gasLimit, in intrinsicGas, spec);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetRemainingGas(in SilaGasPolicy gas) => gas.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetStateReservoir(in SilaGasPolicy gas) => gas.StateReservoir;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetStateGasUsed(in SilaGasPolicy gas) => gas.StateGasUsed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetStateGasSpill(in SilaGasPolicy gas) => gas.StateGasSpill;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong CalculateStateGasSpill(in SilaGasPolicy gas, long stateGasCost)
    {
        if (stateGasCost <= 0)
        {
            return 0;
        }

        long reservoirContribution = gas.StateReservoir;
        if (reservoirContribution <= 0)
        {
            return (ulong)stateGasCost;
        }

        return stateGasCost > reservoirContribution
            ? (ulong)(stateGasCost - reservoirContribution)
            : 0;
    }

    /// <summary>Subtracts <paramref name="cost"/> from the regular gas budget with no affordability check.</summary>
    /// <remarks>The caller must have already proven <c>gas.Value &gt;= cost</c>; otherwise the value underflows.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ConsumeRaw(ref SilaGasPolicy gas, ulong cost) => gas.Value -= cost;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Consume(ref SilaGasPolicy gas, ulong cost)
    {
        if (gas.Value < cost)
        {
            gas.Value = 0;
            gas.OutOfGas = true;
        }
        else
        {
            ConsumeRaw(ref gas, cost);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryConsume(ref SilaGasPolicy gas, ulong cost)
    {
        if (gas.Value < cost) return false;
        ConsumeRaw(ref gas, cost);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConsumeStateGas(ref SilaGasPolicy gas, long stateGasCost)
    {
        long reservoir = gas.StateReservoir;
        if (reservoir >= stateGasCost)
        {
            gas.StateReservoir -= stateGasCost;
            gas.StateGasUsed += stateGasCost;
            return true;
        }

        ulong spillAmount = CalculateStateGasSpill(in gas, stateGasCost);
        if (!TryConsume(ref gas, spillAmount))
        {
            gas.OutOfGas = true;
            return false;
        }

        gas.StateReservoir = Math.Min(0, reservoir);
        gas.StateGasUsed += stateGasCost;
        gas.StateGasSpill += (long)spillAmount;
        return true;
    }

    public static bool TryConsumeStateAndRegularGas(ref SilaGasPolicy gas, long stateGasCost, ulong regularGasCost) =>
        (regularGasCost <= 0 || UpdateGas(ref gas, regularGasCost)) &&
        (stateGasCost <= 0 || ConsumeStateGas(ref gas, stateGasCost));

    public static bool ConsumeSelfDestructGas(ref SilaGasPolicy gas)
        => UpdateGas(ref gas, GasCostOf.SelfDestructSip150);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Refund(ref SilaGasPolicy gas, in SilaGasPolicy childGas)
    {
        gas.Value += childGas.Value;
        gas.StateReservoir += childGas.StateReservoir;
        gas.StateGasUsed += childGas.StateGasUsed;
        gas.StateGasSpill += childGas.StateGasSpill;
        gas.StateGasSpillRefunded += childGas.StateGasSpillRefunded;
    }

    // On explicit REVERT, restore the child's remaining state reservoir plus its reverted
    // state gas usage.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RestoreChildStateGas(ref SilaGasPolicy parentGas, in SilaGasPolicy childGas)
    {
        // Source-based LIFO rollback: net spill refills gas_left, (used - spill) returns to
        // the reservoir; the rolled-back spill counters are not inherited.
        long childNetSpill = GetUnrefundedStateGasSpill(in childGas);
        parentGas.Value += (ulong)childNetSpill;
        parentGas.StateReservoir += childGas.StateReservoir + childGas.StateGasUsed - childNetSpill;
    }

    // On a child exceptional halt state gas produced no durable growth: restore the child's
    // reservoir and usage to the parent reservoir without adding to parent block-state usage.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RestoreChildStateGasOnHalt(ref SilaGasPolicy parentGas, in SilaGasPolicy childGas)
    {
        // On a halt only the reservoir-funded portion returns to the parent; the spilled
        // portion refills gas_left, which the halt burns as regular gas.
        long childNetSpill = GetUnrefundedStateGasSpill(in childGas);
        parentGas.StateReservoir += childGas.StateReservoir + childGas.StateGasUsed - childNetSpill;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RevertRefundToHalt(ref SilaGasPolicy parentGas, in SilaGasPolicy childGas)
    {
        // Code deposit failure halts the child create frame after it merged into the parent;
        // spilled state gas is burned, so only the reservoir-funded portion returns.
        long childNetSpill = GetUnrefundedStateGasSpill(in childGas);
        parentGas.StateReservoir += childGas.StateGasUsed - childNetSpill;
        parentGas.StateGasUsed -= childGas.StateGasUsed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetUnrefundedStateGasSpill(in SilaGasPolicy childGas) =>
        Math.Max(0, childGas.StateGasSpill - childGas.StateGasSpillRefunded);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOutOfGas(in SilaGasPolicy gas) => gas.OutOfGas;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetOutOfGas(ref SilaGasPolicy gas)
    {
        gas.Value = 0;
        gas.OutOfGas = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConsumeAccountAccessGasWithDelegation(ref SilaGasPolicy gas,
        IReleaseSpec spec,
        ref readonly StackAccessTracker accessTracker,
        bool isTracingAccess,
        Address address,
        Address? delegated)
    {
        if (!spec.UseHotAndColdStorage)
            return true;

        bool notOutOfGas = ConsumeAccountAccessGas(ref gas, spec, in accessTracker, isTracingAccess, address);
        return notOutOfGas && (delegated is null || ConsumeAccountAccessGas(ref gas, spec, in accessTracker, isTracingAccess, delegated));
    }

    public static bool ConsumeAccountAccessGas(ref SilaGasPolicy gas,
        IReleaseSpec spec,
        ref readonly StackAccessTracker accessTracker,
        bool isTracingAccess,
        Address address,
        AccountAccessKind kind = AccountAccessKind.Default)
    {
        if (!spec.UseHotAndColdStorage) return true;
        if (isTracingAccess)
        {
            // Ensure that tracing simulates access-list behavior.
            accessTracker.WarmUp(address);
        }

        // WarmUp first so the warm path skips IsPrecompile; precompiles are pre-warmed at tx start.
        return (accessTracker.WarmUp(address) && !spec.IsPrecompile(address)) switch
        {
            true => UpdateGas(ref gas, ColdAccountAccessCost(spec)),
            false when kind == AccountAccessKind.SelfDestructBeneficiary => true,
            false => UpdateGas(ref gas, GasCostOf.WarmStateRead)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ColdAccountAccessCost(IReleaseSpec spec) =>
        spec.IsSip8038Enabled ? Sip8038Constants.ColdAccountAccess : GasCostOf.ColdAccountAccess;

    public static bool ConsumeStorageAccessGas(ref SilaGasPolicy gas,
        ref readonly StackAccessTracker accessTracker,
        bool isTracingAccess,
        in StorageCell storageCell,
        StorageAccessType storageAccessType,
        IReleaseSpec spec)
    {
        if (!spec.UseHotAndColdStorage)
            return true;
        if (isTracingAccess)
        {
            accessTracker.WarmUp(in storageCell);
        }

        if (accessTracker.WarmUp(in storageCell))
            return UpdateGas(ref gas, spec.IsSip8038Enabled ? Sip8038Constants.ColdStorageAccess : GasCostOf.ColdSLoad);
        // SIP-8038 charges the warm-access cost on SSTORE too; the net-metered charge is dropped.
        if (storageAccessType == StorageAccessType.SLOAD || spec.IsSip8038Enabled)
            return UpdateGas(ref gas, GasCostOf.WarmStateRead);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool UpdateMemoryCost(ref SilaGasPolicy gas,
        in UInt256 position,
        in UInt256 length, ref SavmPooledMemory memory)
    {
        ulong memoryCost = memory.CalculateMemoryCost(in position, length, out bool outOfGas);
        if (memoryCost == 0L)
            return !outOfGas;
        return UpdateGas(ref gas, memoryCost);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool UpdateMemoryCost(ref SilaGasPolicy gas,
        in UInt256 position,
        ulong length, ref SavmPooledMemory memory)
    {
        ulong memoryCost = memory.CalculateMemoryCost(in position, length, out bool outOfGas);
        if (memoryCost == 0)
            return !outOfGas;
        return UpdateGas(ref gas, memoryCost);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool UpdateGas(ref SilaGasPolicy gas,
        ulong gasCost)
    {
        if (GetRemainingGas(in gas) < gasCost)
        {
            gas.Value = 0;
            gas.OutOfGas = true;
            return false;
        }

        ConsumeRaw(ref gas, gasCost);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConsumeStorageWrite<TSip8037, TIsSlotCreation>(ref SilaGasPolicy gas, IReleaseSpec spec)
        where TSip8037 : struct, IFlag
        where TIsSlotCreation : struct, IFlag
    {
        // SIP-8038: STORAGE_WRITE is charged on the first change to a slot (fresh or reset).
        if (!TIsSlotCreation.IsActive)
            return UpdateGas(ref gas, spec.IsSip8038Enabled ? Sip8038Constants.StorageWrite : spec.GasCosts.SStoreResetCost);

        ulong regularWriteCost = spec.IsSip8038Enabled ? Sip8038Constants.StorageWrite : GasCostOf.SSetRegular;
        return TSip8037.IsActive switch
        {
            // SIP-8037: charge the regular component first so an OOG halt does not
            // spill state gas into gas_left and then restore it to the parent frame.
            true => TryConsumeStateAndRegularGas(ref gas, GasCostOf.SSetState, regularWriteCost),
            false => UpdateGas(ref gas, GasCostOf.SSet),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateGasUp(ref SilaGasPolicy gas,
        ulong refund) => gas.Value += refund;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RefundStateGas(ref SilaGasPolicy gas, long amount, long stateGasFloor)
        => RefundStateGas(ref gas, amount, stateGasFloor, trackSpillRefund: true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RefundStateGas(ref SilaGasPolicy gas, long amount, long stateGasFloor, bool trackSpillRefund)
    {
        // All callers guard amount >= 0; a negative amount would wrap gas.Value via (ulong)toGasLeft below.
        Debug.Assert(amount >= 0, $"Negative state-gas refund ({amount}).");
        long refundableStateGas = Math.Max(0, gas.StateGasUsed - stateGasFloor);
        long appliedRefund = Math.Min(amount, refundableStateGas);
        long toGasLeft = 0;
        if (trackSpillRefund)
        {
            // Source-based LIFO refill: gas_left first (up to the spill), then the reservoir,
            // so a reverted sub-frame's spill does not inflate it.
            toGasLeft = Math.Min(appliedRefund, GetUnrefundedStateGasSpill(in gas));
            gas.StateGasSpillRefunded += toGasLeft;
        }

        gas.Value += (ulong)toGasLeft;
        gas.StateReservoir += appliedRefund - toGasLeft;
        gas.StateGasUsed -= appliedRefund;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long DiscardStateGas(ref SilaGasPolicy gas, long amount, long stateGasFloor)
    {
        // No StateGasSpillRefunded marking here: the credit-time LIFO refill already marked,
        // and the marks arrive via the regular child-gas merge.
        long discardableStateGas = Math.Max(0, gas.StateGasUsed - stateGasFloor);
        long appliedRefund = Math.Min(amount, discardableStateGas);
        gas.StateGasUsed -= appliedRefund;
        return amount - appliedRefund;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddStateGasRefundToReservoir(ref SilaGasPolicy gas, long amount, bool trackSpillRefund)
    {
        // Continues the source-based LIFO refill: gas_left up to the unrefunded spill, then
        // the reservoir.
        long toGasLeft = trackSpillRefund ? TrackStateGasSpillRefund(ref gas, amount) : 0;
        gas.Value += (ulong)toGasLeft;
        gas.StateReservoir += amount - toGasLeft;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveStateGasRefundFromReservoir(ref SilaGasPolicy gas, long amount)
    {
        // Revoke what is still parked in the reservoir (never fabricating spill debt), then what
        // the credit funded from usage; any remainder refilled a net-spill hole, so restore the debt.
        long fromReservoir = Math.Clamp(gas.StateReservoir, 0, amount);
        gas.StateReservoir -= fromReservoir;
        amount -= fromReservoir;

        if (amount > 0)
        {
            long fromUsed = Math.Min(amount, gas.StateGasUsed);
            gas.StateGasUsed -= fromUsed;
            gas.StateReservoir -= amount - fromUsed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long TrackStateGasSpillRefund(ref SilaGasPolicy gas, long amount)
    {
        long tracked = Math.Min(amount, GetUnrefundedStateGasSpill(in gas));
        gas.StateGasSpillRefunded += tracked;
        return tracked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetForHalt(ref SilaGasPolicy gas, long initialStateReservoir, long initialStateGasUsed)
    {
        // Snap state-gas back to its tx-start shape (reservoir=R0, used=intrinsic floor,
        // spill=0). The post-reset StateGasUsed feeds SpentGas so the user does not keep
        // paying for state-gas that did not commit.
        gas.StateReservoir = initialStateReservoir;
        gas.StateGasUsed = initialStateGasUsed;
        gas.StateGasSpill = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetCodeInsertRegularRefund(ulong codeInsertRefunds, IReleaseSpec spec)
    {
        if (codeInsertRefunds == 0UL) return 0;
        // SIP-8038: per existing authority, the worst-case ACCOUNT_WRITE charged in the intrinsic
        // returns to the refund counter (state refunds are applied in Apply8037DelegationRefunds).
        if (spec.IsSip8038Enabled) return Sip8038Constants.AccountWrite * codeInsertRefunds;
        if (spec.IsSip8037Enabled) return 0;
        return (GasCostOf.NewAccount - GasCostOf.PerAuthBaseCost) * codeInsertRefunds;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ApplyCodeInsertRefunds(ref SilaGasPolicy gas, ulong codeInsertRefunds, IReleaseSpec spec, long stateGasFloor)
        => GetCodeInsertRegularRefund(codeInsertRefunds, spec);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConsumeCallValueTransfer(ref SilaGasPolicy gas)
        => UpdateGas(ref gas, GasCostOf.CallValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConsumeCallValueTransferSip2780(ref SilaGasPolicy gas)
        => UpdateGas(ref gas, Sip8038Constants.CallValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConsumeNewAccountCreation<TSip8037>(ref SilaGasPolicy gas) where TSip8037 : struct, IFlag => TSip8037.IsActive switch
    {
        true => ConsumeStateGas(ref gas, GasCostOf.NewAccountState),
        false => UpdateGas(ref gas, GasCostOf.NewAccount)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConsumeLogEmission(ref SilaGasPolicy gas, ulong topicCount, ulong dataSize)
    {
        ulong cost = GasCostOf.Log + topicCount * GasCostOf.LogTopic + dataSize * GasCostOf.LogData;
        return UpdateGas(ref gas, cost);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ConsumeDataCopyGas(ref SilaGasPolicy gas, IReleaseSpec spec, bool isExternalCode, ulong words)
        => Consume(ref gas, (isExternalCode ? spec.GasCosts.ExtCodeCost : GasCostOf.VeryLow) + GasCostOf.Memory * words);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnBeforeInstructionTrace(in SilaGasPolicy gas, int pc, Instruction instruction, int depth) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnAfterInstructionTrace(in SilaGasPolicy gas) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SilaGasPolicy Max(in SilaGasPolicy a, in SilaGasPolicy b) =>
        a.Value >= b.Value ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong CombineBlockGas(ulong blockRegularGas, ulong blockStateGas) =>
        Math.Max(blockRegularGas, blockStateGas);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SilaGasPolicy CreateChildFrameGas(ref SilaGasPolicy parentGas, ulong childRegularGas)
    {
        long childStateReservoir = parentGas.StateReservoir;
        parentGas.StateReservoir = 0;

        return new SilaGasPolicy
        {
            Value = childRegularGas,
            StateReservoir = childStateReservoir,
            StateGasUsed = 0,
            StateGasSpill = 0,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntrinsicGas<SilaGasPolicy> CalculateIntrinsicGas(Transaction tx, IReleaseSpec spec) =>
        CalculateIntrinsicGas(tx, spec, blockGasLimit: 0);

    public static IntrinsicGas<SilaGasPolicy> CalculateIntrinsicGas(Transaction tx, IReleaseSpec spec, ulong blockGasLimit)
    {
        if (Volatile.Read(ref tx.IntrinsicGasMemo) is IntrinsicGasMemo memo && ReferenceEquals(memo.Spec, spec))
        {
            return memo.Gas;
        }

        IntrinsicGas<SilaGasPolicy> gas = Calculate(tx, spec, blockGasLimit);
        Volatile.Write(ref tx.IntrinsicGasMemo, new IntrinsicGasMemo(spec, gas));
        return gas;
    }

    private sealed record IntrinsicGasMemo(IReleaseSpec Spec, IntrinsicGas<SilaGasPolicy> Gas) : IIntrinsicGasMemo;

    private static IntrinsicGas<SilaGasPolicy> Calculate(Transaction tx, IReleaseSpec spec, ulong blockGasLimit)
    {
        ulong tokensInCallData = IntrinsicGasCalculator.CalculateTokensInCallData(tx, spec);
        ulong floorTokensInAccessList = IntrinsicGasCalculator.CalculateFloorTokensInAccessList(tx, spec);
        (ulong authRegularCost, long authStateCost) = IntrinsicGasCalculator.AuthorizationListCost(tx, spec);
        ulong accessListCost = IntrinsicGasCalculator.AccessListCost(tx, spec, floorTokensInAccessList);

        ulong baseCost = spec.IsSip2780Enabled ? GasCostOf.TransactionSip2780 : GasCostOf.Transaction;
        ulong regularGas = baseCost
                          + DataCost(tx, spec, tokensInCallData)
                          + CreateCost(tx, spec)
                          + accessListCost
                          + authRegularCost
                          + Sip2780ExtraGas(tx, spec);
        ulong floorCost = IntrinsicGasCalculator.CalculateFloorCost(tx, spec, tokensInCallData, floorTokensInAccessList);
        long createStateCost = CreateStateCost(tx, spec);
        long totalStateCost = authStateCost + createStateCost;
        return spec.IsSip8037Enabled
            ? new IntrinsicGas<SilaGasPolicy>(
                new SilaGasPolicy
                {
                    Value = regularGas,
                    StateReservoir = totalStateCost,
                    StateGasUsed = totalStateCost,
                },
                FromULong(floorCost))
            : new IntrinsicGas<SilaGasPolicy>(FromULong(regularGas), FromULong(floorCost));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SilaGasPolicy CreateAvailableFromIntrinsic(ulong gasLimit, in SilaGasPolicy intrinsicGas, IReleaseSpec spec)
    {
        // Callers must validate intrinsic gas against gasLimit (ValidateIntrinsicGas / Sip8037.ExceedsCap)
        // before calling; if they don't, the subtraction wraps silently.
        Debug.Assert(gasLimit >= intrinsicGas.Value + (ulong)intrinsicGas.StateReservoir,
            $"gasLimit ({gasLimit}) < intrinsicRegular ({intrinsicGas.Value}) + intrinsicState ({intrinsicGas.StateReservoir})");
        ulong executionGas = gasLimit - intrinsicGas.Value - (ulong)intrinsicGas.StateReservoir;
        ulong reservoir = 0;

        if (spec.IsSip8037Enabled)
        {
            // SIP-8037: cap gas_left at TX_MAX_GAS_LIMIT - intrinsic_regular, overflow goes to reservoir
            ulong maxGasLeft = Sip7825Constants.DefaultTxGasLimitCap.SaturatingSub(intrinsicGas.Value);
            reservoir = executionGas.SaturatingSub(maxGasLeft);
            executionGas -= reservoir;
        }

        return new SilaGasPolicy
        {
            Value = executionGas,
            StateReservoir = (long)reservoir,
            StateGasUsed = intrinsicGas.StateReservoir,
            StateGasSpill = 0,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateCost(Transaction tx, IReleaseSpec spec) =>
        tx.IsContractCreation && spec.IsSip2Enabled
            ? (spec.IsSip8038Enabled ? Sip8038Constants.CreateAccess
                : spec.IsSip8037Enabled ? GasCostOf.CreateRegular
                : GasCostOf.TxCreate)
            : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long CreateStateCost(Transaction tx, IReleaseSpec spec) =>
        tx.IsContractCreation && spec.IsSip8037Enabled ? GasCostOf.CreateState : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong DataCost(Transaction tx, IReleaseSpec spec, ulong tokensInCallData) =>
        spec.GetBaseDataCost(tx) + tokensInCallData * GasCostOf.TxDataZero;

    /// <summary>
    /// SIP-2780 recipient charge on top of TX_BASE_COST.
    /// </summary>
    /// <remarks>
    /// State-independent by design: a flat cold touch for a non-self recipient, plus transfer-log and
    /// value-move costs on value transfers. New-account and delegation costs are charged elsewhere.
    /// </remarks>
    private static ulong Sip2780ExtraGas(Transaction tx, IReleaseSpec spec)
    {
        if (!spec.IsSip2780Enabled) return 0;

        bool hasValue = !tx.Value.IsZero;

        if (tx.IsContractCreation)
            return hasValue ? GasCostOf.TransferLogSip2780 : 0;

        // Self-transfers coalesce into the sender leaf write already priced into TX_BASE_COST.
        if (tx.SenderAddress == tx.To) return 0;

        ulong cost = Sip8038Constants.ColdAccountAccess;
        if (hasValue)
            cost += GasCostOf.TransferLogSip2780 + GasCostOf.TxValueCostSip2780;

        return cost;
    }
}
