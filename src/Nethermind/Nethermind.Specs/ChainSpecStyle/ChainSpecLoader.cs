// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Nethermind.Config;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Exceptions;
using Nethermind.Core.ExecutionRequest;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Serialization.Json;
using Nethermind.Specs.ChainSpecStyle.Json;

namespace Nethermind.Specs.ChainSpecStyle;

/// <summary>
/// This class can load a Parity-style chain spec file and build a <see cref="ChainSpec"/> out of it.
/// </summary>
public class ChainSpecLoader(IJsonSerializer serializer, ILogManager logManager) : IChainSpecLoader
{
    private readonly ILogger _logger = logManager.GetClassLogger<ChainSpecLoader>();

    public ChainSpec Load(Stream streamData)
    {
        try
        {
            ChainSpecJson chainSpecJson = serializer.Deserialize<ChainSpecJson>(streamData);
            return InitChainSpecFrom(chainSpecJson);
        }
        catch (Exception e)
        {
            throw new InvalidDataException($"Error when loading chainspec ({e.Message})", e);
        }
    }

    private ChainSpec InitChainSpecFrom(ChainSpecJson chainSpecJson)
    {
        ulong networkId = chainSpecJson.Params.NetworkId ?? chainSpecJson.Params.ChainId ?? 1;
        ChainSpec chainSpec = new()
        {
            NetworkId = networkId,
            ChainId = chainSpecJson.Params.ChainId ?? networkId,
            Name = chainSpecJson.Name,
            DataDir = chainSpecJson.DataDir
        };

        // LoadGenesis reads chainSpec.Parameters, which LoadParameters populates and label-expands.
        LoadParameters(chainSpecJson, chainSpec);
        LoadGenesis(chainSpecJson, chainSpec);
        LoadEngine(chainSpecJson, chainSpec);
        LoadAllocations(chainSpecJson, chainSpec);
        LoadBootnodes(chainSpecJson, chainSpec);
        LoadTransitions(chainSpecJson, chainSpec);

        return chainSpec;
    }

    private void LoadParameters(ChainSpecJson chainSpecJson, ChainSpec chainSpec)
    {
        ulong? GetTransitions(string builtInName, Predicate<KeyValuePair<string, JsonElement>> predicate)
        {
            AllocationJson? allocation = chainSpecJson.Accounts?.Values.FirstOrDefault(v => v.BuiltIn?.Name.Equals(builtInName, StringComparison.OrdinalIgnoreCase) == true);
            if (allocation is null) return null;
            KeyValuePair<string, JsonElement>[] pricing = allocation.BuiltIn?.Pricing.Where(o => predicate(o)).ToArray();
            if (pricing?.Length > 0)
            {
                string key = pricing[0].Key;
                return ulong.TryParse(key, out ulong transition) ? transition : Convert.ToUInt64(key, 16);
            }

            return null;
        }

        ulong? GetTransitionForExpectedPricing(string builtInName, string innerPath, long expectedValue)
        {
            bool GetForExpectedPricing(KeyValuePair<string, JsonElement> o) =>
                o.Value.TryGetSubProperty(innerPath, out JsonElement value) && value.GetInt64() == expectedValue;

            return GetTransitions(builtInName, GetForExpectedPricing);
        }

        ulong? GetTransitionIfInnerPathExists(string builtInName, string innerPath)
        {
            bool GetForInnerPathExistence(KeyValuePair<string, JsonElement> o) =>
                o.Value.TryGetSubProperty(innerPath, out _);

            return GetTransitions(builtInName, GetForInnerPathExistence);
        }

        chainSpec.Parameters = new ChainParameters
        {
            GasLimitBoundDivisor = chainSpecJson.Params.GasLimitBoundDivisor ?? 0x0400UL,
            MaximumExtraDataSize = chainSpecJson.Params.MaximumExtraDataSize ?? 32,
            MinGasLimit = chainSpecJson.Params.MinGasLimit ?? 5000UL,
            MinHistoryRetentionEpochs = chainSpecJson.Params.MinHistoryRetentionEpochs ?? 82125,
            MinBalRetentionEpochs = chainSpecJson.Params.MinBalRetentionEpochs ?? 3533,
            MaxCodeSize = chainSpecJson.Params.MaxCodeSize,
            MaxCodeSizeTransition = chainSpecJson.Params.MaxCodeSizeTransition,
            MaxCodeSizeTransitionTimestamp = chainSpecJson.Params.MaxCodeSizeTransitionTimestamp,
            Registrar = chainSpecJson.Params.Registrar,
            ForkBlock = chainSpecJson.Params.ForkBlock,
            ForkCanonHash = chainSpecJson.Params.ForkCanonHash,
            Sip7Transition = chainSpecJson.Params.Sip7Transition,
            Sip150Transition = chainSpecJson.Params.Sip150Transition,
            Sip152Transition = chainSpecJson.Params.Sip152Transition,
            Sip160Transition = chainSpecJson.Params.Sip160Transition,
            Sip161abcTransition = chainSpecJson.Params.Sip161abcTransition,
            Sip161dTransition = chainSpecJson.Params.Sip161dTransition,
            Sip155Transition = chainSpecJson.Params.Sip155Transition,
            Sip140Transition = chainSpecJson.Params.Sip140Transition,
            Sip211Transition = chainSpecJson.Params.Sip211Transition,
            Sip214Transition = chainSpecJson.Params.Sip214Transition,
            Sip658Transition = chainSpecJson.Params.Sip658Transition,
            Sip145Transition = chainSpecJson.Params.Sip145Transition,
            Sip1014Transition = chainSpecJson.Params.Sip1014Transition,
            Sip1052Transition = chainSpecJson.Params.Sip1052Transition,
            Sip1108Transition = chainSpecJson.Params.Sip1108Transition,
            Sip1283Transition = chainSpecJson.Params.Sip1283Transition,
            Sip1283DisableTransition = chainSpecJson.Params.Sip1283DisableTransition,
            Sip1283ReenableTransition = chainSpecJson.Params.Sip1283ReenableTransition,
            Sip1344Transition = chainSpecJson.Params.Sip1344Transition,
            Sip1706Transition = chainSpecJson.Params.Sip1706Transition,
            Sip1884Transition = chainSpecJson.Params.Sip1884Transition,
            Sip2028Transition = chainSpecJson.Params.Sip2028Transition,
            Sip2200Transition = chainSpecJson.Params.Sip2200Transition,
            Sip1559Transition = chainSpecJson.Params.Sip1559Transition,
            Sip2315Transition = chainSpecJson.Params.Sip2315Transition,
            Sip2537Transition = chainSpecJson.Params.Sip2537Transition,
            Sip2565Transition = chainSpecJson.Params.Sip2565Transition,
            Sip2929Transition = chainSpecJson.Params.Sip2929Transition,
            Sip2930Transition = chainSpecJson.Params.Sip2930Transition,
            Sip3198Transition = chainSpecJson.Params.Sip3198Transition,
            Sip3541Transition = chainSpecJson.Params.Sip3541Transition,
            Sip3529Transition = chainSpecJson.Params.Sip3529Transition,
            Sip3607Transition = chainSpecJson.Params.Sip3607Transition,
            BeaconChainGenesisTimestamp = chainSpecJson.Params.BeaconChainGenesisTimestamp,
            Sip1153Transition = chainSpecJson.Params.Sip1153Transition,
            Sip1153TransitionTimestamp = chainSpecJson.Params.Sip1153TransitionTimestamp,
            Sip3651Transition = chainSpecJson.Params.Sip3651Transition,
            Sip3651TransitionTimestamp = chainSpecJson.Params.Sip3651TransitionTimestamp,
            Sip3855Transition = chainSpecJson.Params.Sip3855Transition,
            Sip3855TransitionTimestamp = chainSpecJson.Params.Sip3855TransitionTimestamp,
            Sip3860Transition = chainSpecJson.Params.Sip3860Transition,
            Sip3860TransitionTimestamp = chainSpecJson.Params.Sip3860TransitionTimestamp,
            Sip4895TransitionTimestamp = chainSpecJson.Params.Sip4895TransitionTimestamp,
            Sip4844TransitionTimestamp = chainSpecJson.Params.Sip4844TransitionTimestamp,
            Sip4844Transition = chainSpecJson.Params.Sip4844Transition,
            Sip2537TransitionTimestamp = chainSpecJson.Params.Sip2537TransitionTimestamp,
            Sip5656Transition = chainSpecJson.Params.Sip5656Transition,
            Sip5656TransitionTimestamp = chainSpecJson.Params.Sip5656TransitionTimestamp,
            Sip6780Transition = chainSpecJson.Params.Sip6780Transition,
            Sip6780TransitionTimestamp = chainSpecJson.Params.Sip6780TransitionTimestamp,
            Sip7951TransitionTimestamp = chainSpecJson.Params.Sip7951TransitionTimestamp,
            Rip7212TransitionTimestamp = chainSpecJson.Params.Rip7212TransitionTimestamp,
            Sip4788TransitionTimestamp = chainSpecJson.Params.Sip4788TransitionTimestamp,
            Sip7702Transition = chainSpecJson.Params.Sip7702Transition,
            Sip7702TransitionTimestamp = chainSpecJson.Params.Sip7702TransitionTimestamp,
            Sip7918TransitionTimestamp = chainSpecJson.Params.Sip7918TransitionTimestamp,
            Sip7823TransitionTimestamp = chainSpecJson.Params.Sip7823TransitionTimestamp,
            Sip7825TransitionTimestamp = chainSpecJson.Params.Sip7825TransitionTimestamp,
            Sip4788ContractAddress = chainSpecJson.Params.Sip4788ContractAddress ?? Sip4788Constants.BeaconRootsAddress,
            Sip2935Transition = chainSpecJson.Params.Sip2935Transition,
            Sip2935TransitionTimestamp = chainSpecJson.Params.Sip2935TransitionTimestamp,
            Sip2935ContractAddress = chainSpecJson.Params.Sip2935ContractAddress ?? Sip2935Constants.BlockHashHistoryAddress,
            Sip2935RingBufferSize = chainSpecJson.Params.Sip2935RingBufferSize ?? Sip2935Constants.RingBufferSize,
            TransactionPermissionContract = chainSpecJson.Params.TransactionPermissionContract,
            TransactionPermissionContractTransition = chainSpecJson.Params.TransactionPermissionContractTransition,
            ValidateChainIdTransition = chainSpecJson.Params.ValidateChainIdTransition,
            ValidateReceiptsTransition = chainSpecJson.Params.ValidateReceiptsTransition,
            Sip1559ElasticityMultiplier = chainSpecJson.Params.Sip1559ElasticityMultiplier ?? Sip1559Constants.DefaultElasticityMultiplier,
            Sip1559BaseFeeInitialValue = chainSpecJson.Params.Sip1559BaseFeeInitialValue ?? Sip1559Constants.DefaultForkBaseFee,
            Sip1559BaseFeeMaxChangeDenominator = chainSpecJson.Params.Sip1559BaseFeeMaxChangeDenominator ??
                                                 Sip1559Constants.DefaultBaseFeeMaxChangeDenominator,

            Sip6110TransitionTimestamp = chainSpecJson.Params.Sip6110TransitionTimestamp,
            DepositContractAddress = LoadDependentParam(chainSpecJson.Params.Sip6110TransitionTimestamp, chainSpecJson.Params.DepositContractAddress,
                () => chainSpecJson.Params.ChainId == BlockchainIds.SilaMainnet ? Sip6110Constants.MainnetDepositContractAddress : null),
            Sip7002TransitionTimestamp = chainSpecJson.Params.Sip7002TransitionTimestamp,
            Sip7623Transition = chainSpecJson.Params.Sip7623Transition,
            Sip7623TransitionTimestamp = chainSpecJson.Params.Sip7623TransitionTimestamp,
            Sip7976TransitionTimestamp = chainSpecJson.Params.Sip7976TransitionTimestamp,
            Sip7981TransitionTimestamp = chainSpecJson.Params.Sip7981TransitionTimestamp,
            Sip7883TransitionTimestamp = chainSpecJson.Params.Sip7883TransitionTimestamp,
            Sip7002ContractAddress = chainSpecJson.Params.Sip7002ContractAddress ?? Sip7002Constants.WithdrawalRequestPredeployAddress,
            Sip7251TransitionTimestamp = chainSpecJson.Params.Sip7251TransitionTimestamp,
            Sip7251ContractAddress = chainSpecJson.Params.Sip7251ContractAddress ?? Sip7251Constants.ConsolidationRequestPredeployAddress,
            FeeCollector = chainSpecJson.Params.FeeCollector,
            Sip1559FeeCollectorTransition = chainSpecJson.Params.Sip1559FeeCollectorTransition,
            Sip1559BaseFeeMinValueTransition = chainSpecJson.Params.Sip1559BaseFeeMinValueTransition,
            Sip1559BaseFeeMinValue = chainSpecJson.Params.Sip1559BaseFeeMinValue,
            Sip4844BlobGasPriceUpdateFraction = chainSpecJson.Params.Sip4844BlobGasPriceUpdateFraction,
            Sip4844MinBlobGasPrice = chainSpecJson.Params.Sip4844MinBlobGasPrice,
            Sip4844FeeCollectorTransitionTimestamp = chainSpecJson.Params.Sip4844FeeCollectorTransitionTimestamp,
            MergeForkIdTransition = chainSpecJson.Params.MergeForkIdTransition,
            TerminalTotalDifficulty = chainSpecJson.Params.TerminalTotalDifficulty,
            TerminalPoWBlockNumber = chainSpecJson.Params.TerminalPoWBlockNumber,
            BlobSchedule = chainSpecJson.Params.BlobSchedule,

            Sip7594TransitionTimestamp = chainSpecJson.Params.Sip7594TransitionTimestamp,
            Sip7939TransitionTimestamp = chainSpecJson.Params.Sip7939TransitionTimestamp,

            Sip7934TransitionTimestamp = chainSpecJson.Params.Sip7934TransitionTimestamp,
            Sip7934MaxRlpBlockSize = chainSpecJson.Params.Sip7934MaxRlpBlockSize ?? Sip7934Constants.DefaultMaxRlpBlockSize,

            Sip7778TransitionTimestamp = chainSpecJson.Params.Sip7778TransitionTimestamp,
            Sip8037TransitionTimestamp = chainSpecJson.Params.Sip8037TransitionTimestamp,

            Sip7928TransitionTimestamp = chainSpecJson.Params.Sip7928TransitionTimestamp,
            Sip7708TransitionTimestamp = chainSpecJson.Params.Sip7708TransitionTimestamp,

            Sip8024TransitionTimestamp = chainSpecJson.Params.Sip8024TransitionTimestamp,
            Sip8246TransitionTimestamp = chainSpecJson.Params.Sip8246TransitionTimestamp,
            Sip8038TransitionTimestamp = chainSpecJson.Params.Sip8038TransitionTimestamp,
            Sip8282TransitionTimestamp = chainSpecJson.Params.Sip8282TransitionTimestamp,
            Sip7843TransitionTimestamp = chainSpecJson.Params.Sip7843TransitionTimestamp,
            Sip7954TransitionTimestamp = chainSpecJson.Params.Sip7954TransitionTimestamp,
            Sip2780TransitionTimestamp = chainSpecJson.Params.Sip2780TransitionTimestamp,
        };

        chainSpec.Parameters.ExpandAll(chainSpecJson.Params);
        ValidateParams(chainSpec.Parameters);

        // Pre-SilaShanghai SIPs that are part of the genesis baseline for chains without explicit
        // transitions. Applied AFTER ExpandAll so an explicit chainspec field or fork label wins.
        chainSpec.Parameters.Sip150Transition ??= 0;
        chainSpec.Parameters.Sip160Transition ??= 0;
        chainSpec.Parameters.Sip161abcTransition ??= 0;
        chainSpec.Parameters.Sip161dTransition ??= 0;
        chainSpec.Parameters.Sip155Transition ??= 0;

        chainSpec.Parameters.Sip152Transition ??= GetTransitionForExpectedPricing("blake2_f", "price.blake2_f.gas_per_round", 1);
        chainSpec.Parameters.Sip1108Transition ??= GetTransitionForExpectedPricing("alt_bn128_add", "price.alt_bn128_const_operations.price", 150)
                                                   ?? GetTransitionForExpectedPricing("alt_bn128_mul", "price.alt_bn128_const_operations.price", 6000)
                                                   ?? GetTransitionForExpectedPricing("alt_bn128_pairing", "price.alt_bn128_pairing.base", 45000);
        chainSpec.Parameters.Sip2565Transition ??= GetTransitionIfInnerPathExists("modexp", "price.modexp2565");

        Sip4844Constants.OverrideIfAny(chainSpec.Parameters.Sip4844MinBlobGasPrice);
    }

    internal static TValue? LoadDependentParam<TTransition, TValue>(
        TTransition? transition,
        TValue? value,
        Func<TValue?>? fallback = null,
        [CallerArgumentExpression("transition")] string transitionPropertyName = "",
        [CallerArgumentExpression("value")] string valuePropertyName = "")
        where TTransition : struct, IBinaryInteger<TTransition> =>
        transition is not null
            ? value is null
                ? (fallback is not null ? fallback() : default) ?? throw new InvalidConfigurationException(
                    $"Chainspec contains configuration for {transitionPropertyName}, but doesn't contain it for connected parameter {valuePropertyName}",
                    ExitCodes.MissingChainspecSipConfiguration)
                : value
            : default;

    private static void ValidateParams(ChainParameters parameters)
    {
        if (parameters.Sip1283ReenableTransition != parameters.Sip1706Transition
            && parameters.Sip1283DisableTransition.HasValue)
        {
            throw new InvalidOperationException("When 'Sip1283ReenableTransition' or 'Sip1706Transition' are provided they have to have same value as they are both part of 'Sip2200Transition'.");
        }

        if (parameters.Sip1706Transition.HasValue
            && parameters.Sip2200Transition.HasValue)
        {
            throw new InvalidOperationException("Both 'Sip2200Transition' and 'Sip1706Transition' are provided. Please provide either 'Sip2200Transition' or pair of 'Sip1283ReenableTransition' and 'Sip1706Transition' as they have same meaning.");
        }
    }

    private static void LoadTransitions(ChainSpecJson chainSpecJson, ChainSpec chainSpec)
    {
        chainSpec.HomesteadBlockNumber = 0;
        chainSpec.TangerineWhistleBlockNumber = chainSpec.Parameters.Sip150Transition;
        chainSpec.SpuriousDragonBlockNumber = chainSpec.Parameters.Sip160Transition;
        chainSpec.ByzantiumBlockNumber = chainSpec.Parameters.Sip140Transition;
        chainSpec.ConstantinopleBlockNumber =
            chainSpec.Parameters.Sip1283DisableTransition is null
                ? null
                : chainSpec.Parameters.Sip145Transition;
        chainSpec.ConstantinopleFixBlockNumber =
            chainSpec.Parameters.Sip1283DisableTransition ?? chainSpec.Parameters.Sip145Transition;
        chainSpec.IstanbulBlockNumber = chainSpec.Parameters.Sip2200Transition;
        chainSpec.BerlinBlockNumber = chainSpec.Parameters.Sip2929Transition;
        chainSpec.LondonBlockNumber = chainSpec.Parameters.Sip1559Transition;
        chainSpec.ShanghaiTimestamp = chainSpec.Parameters.Sip3651TransitionTimestamp;
        chainSpec.CancunTimestamp = chainSpec.Parameters.Sip4844TransitionTimestamp;
        chainSpec.PragueTimestamp = chainSpec.Parameters.Sip7002TransitionTimestamp;
        chainSpec.OsakaTimestamp = chainSpec.Parameters.Sip7594TransitionTimestamp;
        chainSpec.AmsterdamTimestamp = chainSpec.Parameters.Sip7928TransitionTimestamp;

        // TheMerge parameters
        chainSpec.MergeForkIdBlockNumber = chainSpec.Parameters.MergeForkIdTransition;
        chainSpec.TerminalPoWBlockNumber = chainSpec.Parameters.TerminalPoWBlockNumber;
        chainSpec.TerminalTotalDifficulty = chainSpec.Parameters.TerminalTotalDifficulty;

        if (chainSpec.EngineChainSpecParametersProvider is not null)
        {
            foreach (IChainSpecEngineParameters chainSpecEngineParameters in chainSpec.EngineChainSpecParametersProvider
                         .AllChainSpecParameters)
            {
                chainSpecEngineParameters.ApplyToChainSpec(chainSpec);
            }
        }
    }

    private void LoadEngine(ChainSpecJson chainSpecJson, ChainSpec chainSpec)
    {
        Dictionary<string, JsonElement> engineParameters = chainSpecJson.Engine.CustomEngineData.ToDictionary(
            engine => engine.Key,
            engine => engine.Value.TryGetProperty("params", out JsonElement value) ? value : engine.Value);

        chainSpec.EngineChainSpecParametersProvider = new ChainSpecParametersProvider(engineParameters, serializer);
        if (string.IsNullOrEmpty(chainSpec.SealEngineType))
        {
            chainSpec.SealEngineType = chainSpec.EngineChainSpecParametersProvider.SealEngineType;
        }

        if (string.IsNullOrEmpty(chainSpec.SealEngineType))
        {
            throw new NotSupportedException("unknown seal engine in chainspec");
        }
    }

    private static void LoadGenesis(ChainSpecJson chainSpecJson, ChainSpec chainSpec)
    {
        if (chainSpecJson.Genesis is null)
        {
            return;
        }

        ulong nonce = chainSpecJson.Genesis.Seal?.Sila?.Nonce ?? 0UL;
        Hash256 mixHash = chainSpecJson.Genesis.Seal?.Sila?.MixHash ?? Keccak.Zero;

        // Engine-specific seal sections are stashed raw; the owning consensus plugin (e.g. AuRa)
        // upgrades Genesis.Header via its ChainSpec interceptor.
        chainSpec.CustomSeal = chainSpecJson.Genesis.Seal?.CustomSeal;

        Hash256 parentHash = chainSpecJson.Genesis.ParentHash ?? Keccak.Zero;
        ulong timestamp = chainSpecJson.Genesis.Timestamp;
        UInt256 difficulty = chainSpecJson.Genesis.Difficulty;
        byte[] extraData = chainSpecJson.Genesis.ExtraData ?? [];
        ulong gasLimit = chainSpecJson.Genesis.GasLimit;
        Address beneficiary = chainSpecJson.Genesis.Author ?? Address.Zero;
        ChainParameters parameters = chainSpec.Parameters;
        UInt256 baseFee = parameters.Sip1559Transition switch
        {
            null => chainSpecJson.Genesis.BaseFeePerGas ?? UInt256.Zero,
            0 => chainSpecJson.Genesis.BaseFeePerGas ?? Sip1559Constants.DefaultForkBaseFee,
            _ => UInt256.Zero,
        };


        Hash256 stateRoot = chainSpecJson.Genesis.StateRoot ?? Keccak.EmptyTreeHash;
        chainSpec.GenesisStateUnavailable = chainSpecJson.Genesis.StateUnavailable;

        BlockHeader genesisHeader = new(
            parentHash,
            Keccak.OfAnEmptySequenceRlp,
            beneficiary,
            difficulty,
            0,
            gasLimit,
            timestamp,
            extraData)
        {
            Author = beneficiary,
            Hash = Keccak.Zero, // need to run the block to know the actual hash
            Bloom = Bloom.Empty,
            MixHash = mixHash,
            Nonce = nonce,
            ReceiptsRoot = Keccak.EmptyTreeHash,
            StateRoot = stateRoot,
            TxRoot = Keccak.EmptyTreeHash,
            BaseFeePerGas = baseFee
        };

        bool withdrawalsEnabled = parameters.Sip4895TransitionTimestamp is not null && genesisHeader.Timestamp >= parameters.Sip4895TransitionTimestamp;
        bool depositsEnabled = parameters.Sip6110TransitionTimestamp is not null && genesisHeader.Timestamp >= parameters.Sip6110TransitionTimestamp;
        bool withdrawalRequestsEnabled = parameters.Sip7002TransitionTimestamp is not null && genesisHeader.Timestamp >= parameters.Sip7002TransitionTimestamp;
        bool consolidationRequestsEnabled = parameters.Sip7251TransitionTimestamp is not null && genesisHeader.Timestamp >= parameters.Sip7251TransitionTimestamp;
        bool blockAccessListsEnabled = parameters.Sip7928TransitionTimestamp is not null && genesisHeader.Timestamp >= parameters.Sip7928TransitionTimestamp;
        bool slotNumberEnabled = parameters.Sip7843TransitionTimestamp is not null && genesisHeader.Timestamp >= parameters.Sip7843TransitionTimestamp;

        if (withdrawalsEnabled)
        {
            genesisHeader.WithdrawalsRoot = Keccak.EmptyTreeHash;
        }

        bool requestsEnabled = depositsEnabled || withdrawalRequestsEnabled || consolidationRequestsEnabled;
        if (requestsEnabled)
        {
            genesisHeader.RequestsHash = ExecutionRequestExtensions.EmptyRequestsHash;
        }

        bool isSip4844Enabled = parameters.Sip4844TransitionTimestamp is not null && genesisHeader.Timestamp >= parameters.Sip4844TransitionTimestamp;
        if (isSip4844Enabled)
        {
            genesisHeader.BlobGasUsed = chainSpecJson.Genesis.BlobGasUsed;
            genesisHeader.ExcessBlobGas = chainSpecJson.Genesis.ExcessBlobGas;
        }

        bool isSip4788Enabled = parameters.Sip4788TransitionTimestamp is not null && genesisHeader.Timestamp >= parameters.Sip4788TransitionTimestamp;
        if (isSip4788Enabled)
        {
            genesisHeader.ParentBeaconBlockRoot = Keccak.Zero;
        }

        if (requestsEnabled)
        {
            genesisHeader.ReceiptsRoot = Keccak.EmptyTreeHash;
        }

        if (blockAccessListsEnabled)
        {
            genesisHeader.BlockAccessListHash = Keccak.OfAnEmptySequenceRlp;
        }

        if (slotNumberEnabled)
        {
            genesisHeader.SlotNumber = chainSpecJson.Genesis.SlotNumber ?? 0;
        }

        chainSpec.Genesis = !blockAccessListsEnabled ?
            (!withdrawalsEnabled
                ? new Block(genesisHeader)
                : new Block(genesisHeader, [], [], []))
            : new Block(genesisHeader, [], [], [], new());
    }

    private static void LoadAllocations(ChainSpecJson chainSpecJson, ChainSpec chainSpec)
    {
        if (chainSpecJson.Accounts is null)
        {
            return;
        }

        if (chainSpecJson.CodeHashes is not null)
        {
            foreach (KeyValuePair<string, byte[]> codeHash in chainSpecJson.CodeHashes)
            {
                if (ValueKeccak.Compute(codeHash.Value) != new ValueHash256(codeHash.Key)) throw new ArgumentException($"Unexpected code {codeHash.Key}");
            }
            chainSpecJson.CodeHashes[Hash256.Zero.ToString()] = [];
        }

        chainSpec.Allocations = [];
        foreach (KeyValuePair<string, AllocationJson> account in chainSpecJson.Accounts)
        {
            if (account.Value.BuiltIn is not null && account.Value.Balance is null)
            {
                continue;
            }

            if (account.Value.CodeHash is not null && account.Value.Code is not null)
            {
                throw new ArgumentException("CodeHash and Code are both not null");
            }

            Address address = new(account.Key);

            if (account.Value.CodeHash is not null)
            {
                string codeHashString = account.Value.CodeHash.ToString();
                if (chainSpecJson.CodeHashes is null || !chainSpecJson.CodeHashes.TryGetValue(codeHashString, out byte[] codeHash)) throw new ArgumentException($"CodeHash {account.Value.CodeHash} is not found");
                chainSpec.Allocations[address] = new ChainSpecAllocation(
                    account.Value.Balance ?? UInt256.Zero,
                    account.Value.Nonce,
                    codeHash,
                    account.Value.Constructor,
                    account.Value.GetConvertedStorage());
            }
            else
            {
                chainSpec.Allocations[address] = new ChainSpecAllocation(
                    account.Value.Balance ?? UInt256.Zero,
                    account.Value.Nonce,
                    account.Value.Code,
                    account.Value.Constructor,
                    account.Value.GetConvertedStorage());
            }
        }
    }

    private void LoadBootnodes(ChainSpecJson chainSpecJson, ChainSpec chainSpec)
        => chainSpec.Bootnodes = NetworkNode.ParseNodes(chainSpecJson.Nodes, _logger);
}
