// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Int256;
using Nethermind.Serialization.Json;

[assembly: InternalsVisibleTo("Nethermind.Specs.Test")]
[assembly: InternalsVisibleTo("Nethermind.TxPool.Test")]
namespace Nethermind.Specs.ChainSpecStyle.Json;

public class ChainSpecParamsJson : IHasNamedForks
{
    public ulong? ChainId { get; set; }
    public ulong? NetworkId { get; set; }

    public Address Registrar { get; set; }

    public ulong? GasLimitBoundDivisor { get; set; }

    public long? MaximumExtraDataSize { get; set; }

    public ulong? MinGasLimit { get; set; }

    public ulong? MinHistoryRetentionEpochs { get; set; }

    public ulong? MinBalRetentionEpochs { get; set; }

    public ulong? ForkBlock { get; set; }

    public Hash256 ForkCanonHash { get; set; }

    public ulong? Sip7Transition { get; set; }

    public ulong? Sip150Transition { get; set; }

    public ulong? Sip152Transition { get; set; }

    public ulong? Sip160Transition { get; set; }

    public ulong? Sip161abcTransition { get; set; }

    public ulong? Sip161dTransition { get; set; }

    public ulong? Sip155Transition { get; set; }

    public long? MaxCodeSize { get; set; }

    public ulong? MaxCodeSizeTransition { get; set; }

    public ulong? MaxCodeSizeTransitionTimestamp { get; set; }

    public ulong? Sip140Transition { get; set; }

    public ulong? Sip211Transition { get; set; }

    public ulong? Sip214Transition { get; set; }

    public ulong? Sip658Transition { get; set; }

    public ulong? Sip145Transition { get; set; }

    public ulong? Sip1014Transition { get; set; }

    public ulong? Sip1052Transition { get; set; }

    public ulong? Sip1108Transition { get; set; }

    public ulong? Sip1283Transition { get; set; }

    public ulong? Sip1283DisableTransition { get; set; }

    public ulong? Sip1283ReenableTransition { get; set; }

    public ulong? Sip1344Transition { get; set; }

    public ulong? Sip1706Transition { get; set; }

    public ulong? Sip1884Transition { get; set; }

    public ulong? Sip2028Transition { get; set; }

    public ulong? Sip2200Transition { get; set; }

    public ulong? Sip1559Transition { get; set; }

    public ulong? Sip2315Transition { get; set; }

    public ulong? Sip2537Transition { get; set; }

    public ulong? Sip2565Transition { get; set; }

    public ulong? Sip2929Transition { get; set; }

    public ulong? Sip2930Transition { get; set; }

    public ulong? Sip3198Transition { get; set; }

    public ulong? Sip3529Transition { get; set; }

    public ulong? Sip3541Transition { get; set; }

    // We explicitly want this to be enabled by default on all the networks
    // we can disable it if needed, but its expected not to cause issues
    public ulong? Sip3607Transition { get; set; } = 0;

    public UInt256? Sip1559BaseFeeInitialValue { get; set; }

    public UInt256? Sip1559BaseFeeMaxChangeDenominator { get; set; }

    public ulong? Sip1559ElasticityMultiplier { get; set; }

    public Address TransactionPermissionContract { get; set; }

    public ulong? TransactionPermissionContractTransition { get; set; }

    public ulong? ValidateChainIdTransition { get; set; }

    public ulong? ValidateReceiptsTransition { get; set; }

    public ulong? Sip1559FeeCollectorTransition { get; set; }

    public Address FeeCollector { get; set; }

    public ulong? Sip1559BaseFeeMinValueTransition { get; set; }

    public UInt256? Sip1559BaseFeeMinValue { get; set; }

    public ulong? MergeForkIdTransition { get; set; }

    public UInt256? TerminalTotalDifficulty { get; set; }

    public ulong? TerminalPoWBlockNumber { get; set; }
    public ulong? BeaconChainGenesisTimestamp { get; set; }

    public ulong? Sip1153Transition { get; set; }
    public ulong? Sip1153TransitionTimestamp { get; set; }
    public ulong? Sip3651Transition { get; set; }
    public ulong? Sip3651TransitionTimestamp { get; set; }
    public ulong? Sip3855Transition { get; set; }
    public ulong? Sip3855TransitionTimestamp { get; set; }
    public ulong? Sip3860Transition { get; set; }
    public ulong? Sip3860TransitionTimestamp { get; set; }
    public ulong? Sip4895TransitionTimestamp { get; set; }
    public ulong? Sip4844Transition { get; set; }
    public ulong? Sip4844TransitionTimestamp { get; set; }
    public ulong? Sip2537TransitionTimestamp { get; set; }
    public ulong? Sip5656Transition { get; set; }
    public ulong? Sip5656TransitionTimestamp { get; set; }
    public ulong? Sip6780Transition { get; set; }
    public ulong? Sip6780TransitionTimestamp { get; set; }
    public ulong? Sip4788TransitionTimestamp { get; set; }
    public Address Sip4788ContractAddress { get; set; }
    public ulong? Sip2935Transition { get; set; }
    public ulong? Sip2935TransitionTimestamp { get; set; }
    public Address Sip2935ContractAddress { get; set; }
    public ulong? Sip2935RingBufferSize { get; set; }
    public ulong? Sip4844BlobGasPriceUpdateFraction { get; set; }
    public UInt256? Sip4844MinBlobGasPrice { get; set; }
    public ulong? Sip4844FeeCollectorTransitionTimestamp { get; set; }
    public ulong? Sip6110TransitionTimestamp { get; set; }
    public Address DepositContractAddress { get; set; }
    public ulong? Sip7002TransitionTimestamp { get; set; }
    public ulong? Sip7623Transition { get; set; }
    public ulong? Sip7623TransitionTimestamp { get; set; }
    public ulong? Sip7976TransitionTimestamp { get; set; }
    public ulong? Sip7981TransitionTimestamp { get; set; }
    public Address Sip7002ContractAddress { get; set; }
    public ulong? Sip7251TransitionTimestamp { get; set; }
    public Address Sip7251ContractAddress { get; set; }
    public ulong? Sip7951TransitionTimestamp { get; set; }
    public ulong? Rip7212TransitionTimestamp { get; set; }
    public ulong? Sip7702Transition { get; set; }
    public ulong? Sip7702TransitionTimestamp { get; set; }
    public ulong? Sip7883TransitionTimestamp { get; set; }
    public ulong? Sip7823TransitionTimestamp { get; set; }
    public ulong? Sip7825TransitionTimestamp { get; set; }
    public ulong? Sip7918TransitionTimestamp { get; set; }
    public ulong? Sip7934TransitionTimestamp { get; set; }
    public int? Sip7934MaxRlpBlockSize { get; set; }

    public SortedSet<BlobScheduleSettings> BlobSchedule { get; set; } = [];
    public ulong? Sip7594TransitionTimestamp { get; set; }
    public ulong? Sip7939TransitionTimestamp { get; set; }
    public ulong? Sip8037TransitionTimestamp { get; set; }
    public ulong? Sip7778TransitionTimestamp { get; set; }

    public ulong? Sip7928TransitionTimestamp { get; set; }
    public ulong? Sip7708TransitionTimestamp { get; set; }
    public ulong? Sip8024TransitionTimestamp { get; set; }
    public ulong? Sip8246TransitionTimestamp { get; set; }
    public ulong? Sip8038TransitionTimestamp { get; set; }
    public ulong? Sip8282TransitionTimestamp { get; set; }
    public ulong? Sip7843TransitionTimestamp { get; set; }
    public ulong? Sip7954TransitionTimestamp { get; set; }
    public ulong? Sip2780TransitionTimestamp { get; set; }

    /// <summary>
    /// Catch-all for top-level chainspec params keys that don't map to an explicit property —
    /// in practice the hardfork shorthand labels (<c>shanghai</c>, <c>cancun</c>, <c>prague</c>,
    /// <c>osaka</c>, <c>amsterdam</c>, <c>homestead</c>, <c>tangerineWhistle</c>,
    /// <c>spuriousDragon</c>, <c>byzantium</c>, <c>constantinople</c>, <c>petersburg</c>,
    /// <c>istanbul</c>, <c>berlin</c>, <c>london</c>). <see cref="HardforkLabels.ExpandAll"/>
    /// consumes each recognized entry and expands it into the per-SIP transition fields above;
    /// anything still present after expansion is an unknown/typo key.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? NamedForks { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    private Dictionary<string, ulong>? _namedForkBlocks;

    [JsonIgnore]
    private Dictionary<string, ulong>? _namedForkTimestamps;

    IReadOnlyDictionary<string, ulong>? IHasNamedForks.NamedForkBlocks
        => _namedForkBlocks ??= Project<ulong>(HardforkLabelKind.Block);

    IReadOnlyDictionary<string, ulong>? IHasNamedForks.NamedForkTimestamps
        => _namedForkTimestamps ??= Project<ulong>(HardforkLabelKind.Timestamp);

    /// <summary>
    /// Parses the <c>[JsonExtensionData]</c> entries whose keys match a <see cref="HardforkLabels"/>
    /// label of the given <paramref name="kind"/> into a typed lookup.
    /// </summary>
    private Dictionary<string, T>? Project<T>(HardforkLabelKind kind) where T : struct
    {
        if (NamedForks is null or { Count: 0 }) return null;
        Dictionary<string, T>? result = null;
        foreach (IHardforkLabel label in HardforkLabels.All)
        {
            if (label.Kind == kind && NamedForks.TryGetValue(label.LabelName, out JsonElement element))
            {
                result ??= new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
                result[label.LabelName] = element.Deserialize<T>(SilaJsonSerializer.JsonOptions);
            }
        }
        return result;
    }
}
