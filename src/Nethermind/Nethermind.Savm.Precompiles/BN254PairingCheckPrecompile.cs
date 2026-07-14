// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Specs;

namespace Nethermind.Savm.Precompiles;

/// <summary>
/// <see href="https://sips.sila.org/SIPS/sip-197" />
/// </summary>
public partial class BN254PairingCheckPrecompile : IPrecompile<BN254PairingCheckPrecompile>
{
    public static BN254PairingCheckPrecompile Instance { get; } = new();

    public static Address Address { get; } = Address.FromNumber(8);

    /// <see href="https://sips.sila.org/SIPS/sip-7910" />
    public static string Name => "BN254_PAIRING";

    /// <see href="https://sips.sila.org/SIPS/sip-1108" />
    public ulong BaseGasCost(IReleaseSpec releaseSpec) => releaseSpec.IsSip1108Enabled ? 45_000UL : 100_000UL;

    public ulong DataGasCost(ReadOnlyMemory<byte> inputData, IReleaseSpec releaseSpec) =>
        (releaseSpec.IsSip1108Enabled ? 34_000UL : 80_000UL) * (ulong)(inputData.Length / BN254.PairSize);

    public partial Result<byte[]> Run(ReadOnlyMemory<byte> inputData, IReleaseSpec _);

    private static bool ValidateInputLength(ReadOnlyMemory<byte> inputData) =>
        inputData.Length % BN254.PairSize == 0;
}
