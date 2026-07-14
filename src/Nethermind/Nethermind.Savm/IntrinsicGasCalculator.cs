// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Nethermind.Core;
using Nethermind.Core.Sip2930;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Savm.GasPolicy;

namespace Nethermind.Savm;

/// <summary>
/// Non-generic intrinsic gas result for backward compatibility.
/// </summary>
public readonly record struct SilaIntrinsicGas(ulong Standard, ulong FloorGas)
{
    public ulong MinimalGas { get; } = Math.Max(Standard, FloorGas);
    public static explicit operator ulong(SilaIntrinsicGas gas) => gas.MinimalGas;
    public static implicit operator SilaIntrinsicGas(IntrinsicGas<SilaGasPolicy> gas) =>
        new(gas.Standard.Value + (ulong)gas.Standard.StateReservoir, gas.FloorGas.Value);
}

public static class IntrinsicGasCalculator
{
    /// <summary>
    /// Calculates intrinsic gas with TGasPolicy type, allowing MultiGas breakdown for Arbitrum.
    /// </summary>
    private static IntrinsicGas<TGasPolicy> Calculate<TGasPolicy>(Transaction transaction, IReleaseSpec releaseSpec, ulong blockGasLimit = 0)
        where TGasPolicy : struct, IGasPolicy<TGasPolicy> =>
        TGasPolicy.CalculateIntrinsicGas(transaction, releaseSpec, blockGasLimit);

    /// <summary>
    /// Non-generic backward-compatible Calculate method.
    /// </summary>
    public static SilaIntrinsicGas Calculate(Transaction transaction, IReleaseSpec releaseSpec, ulong blockGasLimit = 0) =>
        Calculate<SilaGasPolicy>(transaction, releaseSpec, blockGasLimit);

    public static ulong AccessListCost(Transaction transaction, IReleaseSpec releaseSpec) =>
        AccessListCost(transaction, releaseSpec, CalculateFloorTokensInAccessList(transaction, releaseSpec));

    internal static ulong CalculateTokensInCallData(Transaction transaction, IReleaseSpec spec)
    {
        ReadOnlySpan<byte> data = transaction.Data.Span;
        int totalZeros = data.CountZeros();
        return (ulong)totalZeros + (ulong)(data.Length - totalZeros) * spec.GasCosts.TxDataNonZeroMultiplier;
    }

    // 0 when floor pricing is not active.
    internal static ulong CalculateFloorTokensInAccessList(Transaction transaction, IReleaseSpec spec) =>
        spec.IsSip7981Enabled && transaction.AccessList is { Count: (int addressesCount, int storageKeysCount) }
            ? (ulong)(addressesCount * Address.Size + storageKeysCount * AccessList.StorageKeySize) * spec.GasCosts.TxDataNonZeroMultiplier
            : 0;

    internal static ulong AccessListCost(Transaction transaction, IReleaseSpec spec, ulong floorTokensInAccessList)
    {
        AccessList? accessList = transaction.AccessList;
        if (accessList is null) return 0;

        if (!spec.UseTxAccessLists)
        {
            ThrowInvalidDataException(spec);
        }

        (int addressesCount, int storageKeysCount) = accessList.Count;
        // SIP-8038 realigns access-list entry costs with the cold-access costs they pre-warm.
        ulong addressCost = spec.IsSip8038Enabled ? Sip8038Constants.AccessListAddressCost : GasCostOf.AccessAccountListEntry;
        ulong storageKeyCost = spec.IsSip8038Enabled ? Sip8038Constants.AccessListStorageKeyCost : GasCostOf.AccessStorageListEntry;
        return (ulong)addressesCount * addressCost
            + (ulong)storageKeysCount * storageKeyCost
            + spec.GasCosts.TotalCostFloorPerToken * floorTokensInAccessList;

        [DoesNotReturn, StackTraceHidden]
        static void ThrowInvalidDataException(IReleaseSpec spec) =>
            throw new InvalidDataException($"Transaction with an access list received within the context of {spec.Name}. SIP-2930 is not enabled.");
    }

    internal static (ulong RegularCost, long StateCost) AuthorizationListCost(Transaction transaction, IReleaseSpec spec)
    {
        AuthorizationTuple[]? authList = transaction.AuthorizationList;
        if (authList is null)
        {
            return (0, 0);
        }

        if (!spec.IsAuthorizationListEnabled)
        {
            ThrowAuthorizationListNotEnabled(spec);
        }

        ulong authCount = (ulong)authList.Length;
        ulong perAuthRegular = spec.IsSip8038Enabled ? Sip8038Constants.PerAuthBaseRegular : GasCostOf.PerAuthBaseRegular;
        return spec.IsSip8037Enabled
            ? (
                authCount * perAuthRegular,
                authList.Length * (GasCostOf.NewAccountState + GasCostOf.PerAuthBaseState)
            )
            : (authCount * GasCostOf.NewAccount, 0);

        [DoesNotReturn, StackTraceHidden]
        static void ThrowAuthorizationListNotEnabled(IReleaseSpec releaseSpec) =>
            throw new InvalidDataException($"Transaction with an authorization list received within the context of {releaseSpec.Name}. SIP-7702 is not enabled.");
    }

    private static ulong CalculateFloorTokensInCallData(Transaction transaction, IReleaseSpec spec) =>
        (ulong)transaction.Data.Length * spec.GasCosts.TxDataNonZeroMultiplier;

    private static ulong CalculateSip7623FloorTokensInCallData(Transaction transaction, IReleaseSpec spec, ulong tokensInCallData)
    {
        if (spec.IsSip2028Enabled) return tokensInCallData;

        ReadOnlySpan<byte> data = transaction.Data.Span;
        ulong totalZeros = (ulong)data.CountZeros();
        return totalZeros + ((ulong)data.Length - totalZeros) * GasCostOf.TxDataNonZeroMultiplierSip2028;
    }

    internal static ulong CalculateFloorCost(Transaction transaction, IReleaseSpec spec, ulong tokensInCallData, ulong floorTokensInAccessList)
    {
        // The floor tracks the reduced SIP-2780 base, else the legacy floor would dominate.
        ulong floorBase = spec.IsSip2780Enabled ? GasCostOf.TransactionSip2780 : GasCostOf.Transaction;
        return spec switch
        {
            { IsSip7976Enabled: true } => floorBase + (CalculateFloorTokensInCallData(transaction, spec) + floorTokensInAccessList) * spec.GasCosts.TotalCostFloorPerToken,
            { IsSip7623Enabled: true } => floorBase + CalculateSip7623FloorTokensInCallData(transaction, spec, tokensInCallData) * spec.GasCosts.TotalCostFloorPerToken,
            _ => 0
        };
    }
}
