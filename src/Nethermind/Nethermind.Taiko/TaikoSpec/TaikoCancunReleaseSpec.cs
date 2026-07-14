// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Frozen;
using Nethermind.Core;
using Nethermind.Specs.Forks;
using Nethermind.Taiko.ZkGas;

namespace Nethermind.Taiko.TaikoSpec;

// Test-only Taiko spec stubs. Production code uses `TaikoReleaseSpec` built from
// the chainspec via `TaikoChainSpecBasedSpecProvider`. Each Taiko fork is pinned to
// its corresponding SAVM base: Ontake/Pacaya/Shasta → SilaShanghai, Unzen → SilaOsaka.

/// <summary>Test-only spec for the Ontake fork (SAVM = SilaShanghai).</summary>
public class TaikoOntakeReleaseSpec : SilaShanghai, ITaikoReleaseSpec
{
    /// <summary>Initializes the spec with <see cref="IsOntakeEnabled"/> set.</summary>
    public TaikoOntakeReleaseSpec() => IsOntakeEnabled = true;

    public bool IsOntakeEnabled { get; set; }
    public bool IsPacayaEnabled { get; set; }
    public bool IsShastaEnabled { get; set; }
    public bool IsUnzenEnabled { get; set; }
    public ulong UnzenBlockZkGasLimit { get; set; } = ZkGasSchedule.BlockZkGasLimit;
    public ulong UnzenTxIntrinsicZkGas { get; set; } = ZkGasSchedule.TxIntrinsicZkGas;
    public ReadOnlyMemory<ushort> UnzenOpcodeZkGasMultipliers { get; set; }
    public FrozenDictionary<AddressAsKey, ushort> UnzenPrecompileZkGasMultipliers { get; set; } = FrozenDictionary<AddressAsKey, ushort>.Empty;
    public bool UseSurgeGasPriceOracle { get; set; }
    public Address TaikoL2Address { get; set; } = new("0x1670000000000000000000000000000000010001");
    public bool IsRip7728Enabled { get; set; }
    public bool IsL1StaticCallEnabled { get; set; }

    /// <inheritdoc />
    public override FrozenSet<AddressAsKey> BuildPrecompilesCache() =>
        ITaikoReleaseSpec.BuildTaikoPrecompilesCache(base.BuildPrecompilesCache(), IsRip7728Enabled, IsL1StaticCallEnabled);
}

/// <summary>Test-only spec for the Pacaya fork (SAVM = SilaShanghai).</summary>
public class TaikoPacayaReleaseSpec : TaikoOntakeReleaseSpec
{
    /// <summary>Initializes the spec with <see cref="ITaikoReleaseSpec.IsPacayaEnabled"/> set.</summary>
    public TaikoPacayaReleaseSpec() => IsPacayaEnabled = true;
}

/// <summary>Test-only spec for the Shasta fork (SAVM = SilaShanghai).</summary>
public class TaikoShastaReleaseSpec : TaikoPacayaReleaseSpec
{
    /// <summary>Initializes the spec with <see cref="ITaikoReleaseSpec.IsShastaEnabled"/> set.</summary>
    public TaikoShastaReleaseSpec() => IsShastaEnabled = true;
}

/// <summary>Test-only spec for the Unzen fork (SAVM = SilaOsaka, transitively activates SilaCancun + SilaPrague SIPs).</summary>
public class TaikoUnzenReleaseSpec : SilaOsaka, ITaikoReleaseSpec
{
    /// <summary>Initializes the spec with all Taiko-fork flags up to Unzen enabled.</summary>
    public TaikoUnzenReleaseSpec()
    {
        IsOntakeEnabled = true;
        IsPacayaEnabled = true;
        IsShastaEnabled = true;
        IsUnzenEnabled = true;
    }

    public bool IsOntakeEnabled { get; set; }
    public bool IsPacayaEnabled { get; set; }
    public bool IsShastaEnabled { get; set; }
    public bool IsUnzenEnabled { get; set; }
    public ulong UnzenBlockZkGasLimit { get; set; } = ZkGasSchedule.BlockZkGasLimit;
    public ulong UnzenTxIntrinsicZkGas { get; set; } = ZkGasSchedule.TxIntrinsicZkGas;
    public ReadOnlyMemory<ushort> UnzenOpcodeZkGasMultipliers { get; set; }
    public FrozenDictionary<AddressAsKey, ushort> UnzenPrecompileZkGasMultipliers { get; set; } = FrozenDictionary<AddressAsKey, ushort>.Empty;
    public bool UseSurgeGasPriceOracle { get; set; }
    public Address TaikoL2Address { get; set; } = new("0x1670000000000000000000000000000000010001");
    public bool IsRip7728Enabled { get; set; }
    public bool IsL1StaticCallEnabled { get; set; }

    /// <inheritdoc />
    public override FrozenSet<AddressAsKey> BuildPrecompilesCache() =>
        ITaikoReleaseSpec.BuildTaikoPrecompilesCache(base.BuildPrecompilesCache(), IsRip7728Enabled, IsL1StaticCallEnabled);
}
