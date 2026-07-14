// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Specs.ChainSpecStyle.Json;

namespace Nethermind.Specs.ChainSpecStyle
{
    public class ChainSpecBasedSpecProvider : SpecProviderBase, IForkAwareSpecProvider
    {
        private readonly ChainSpec _chainSpec;
        private IForkAwareSpecProvider? _forkAware;

        public ChainSpecBasedSpecProvider(ChainSpec chainSpec, ILogManager? logManager = null)
            : base(logManager?.GetClassLogger<ChainSpecBasedSpecProvider>() ?? LimboTraceLogger.Instance)
        {
            _chainSpec = chainSpec ?? throw new ArgumentNullException(nameof(chainSpec));
            BuildTransitions();
        }

        public bool GenesisStateUnavailable { get => _chainSpec.GenesisStateUnavailable; }

        protected virtual ReleaseSpec CreateEmptyReleaseSpec() => new();

        private void BuildTransitions()
        {
            SortedSet<ulong> transitionBlockNumbers = [];
            SortedSet<ulong> transitionTimestamps = [];
            transitionBlockNumbers.Add(0);

            foreach (IChainSpecEngineParameters item in _chainSpec.EngineChainSpecParametersProvider
                         .AllChainSpecParameters)
            {
                item.AddTransitions(transitionBlockNumbers, transitionTimestamps);
            }

            AddTransitions(transitionBlockNumbers, _chainSpec, static n => n.EndsWith("BlockNumber") && n != "TerminalPoWBlockNumber");
            AddTransitions(transitionBlockNumbers, _chainSpec.Parameters, static n => n.EndsWith("Transition"));
            AddTransitions(transitionTimestamps, _chainSpec.Parameters, static n => n.EndsWith("TransitionTimestamp"), _chainSpec.Genesis?.Timestamp ?? 0);
            AddBlobScheduleTransitions(transitionTimestamps, _chainSpec);
            TimestampFork = transitionTimestamps.Count > 0 ? transitionTimestamps.Min : ISpecProvider.TimestampForkNever;

            // Scans properties of type T / T? on value whose names pass the filter.
            static void AddTransitions<T>(
                SortedSet<T> transitions,
                object value,
                Func<string, bool> matchPropertyName, T? minValueExclusive = null)
                where T : struct, INumber<T>
            {
                static void Add(SortedSet<T> transitions, T value, T? minValueExclusive)
                {
                    if (minValueExclusive is null || value > minValueExclusive)
                    {
                        transitions.Add(value);
                    }
                }

                if (value is not null)
                {
                    IEnumerable<PropertyInfo> properties = value.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (PropertyInfo propertyInfo in properties)
                    {
                        if (matchPropertyName(propertyInfo.Name))
                        {
                            if (propertyInfo.PropertyType == typeof(T))
                            {
                                Add(transitions, (T)propertyInfo.GetValue(value)!, minValueExclusive);
                            }
                            else if (propertyInfo.PropertyType == typeof(T?))
                            {
                                T? optionalTransition = (T?)propertyInfo.GetValue(value);
                                if (optionalTransition is not null)
                                {
                                    Add(transitions, optionalTransition.Value, minValueExclusive);
                                }
                            }
                        }
                    }
                }
            }

            static void AddBlobScheduleTransitions(SortedSet<ulong> transitions, ChainSpec chainSpec)
            {
                if (chainSpec.Parameters.BlobSchedule is not { Count: > 0 })
                {
                    return;
                }

                ulong genesisTimestamp = chainSpec.Genesis?.Timestamp ?? 0;
                ulong sip4844Timestamp = chainSpec.Parameters.Sip4844TransitionTimestamp
                    ?? throw new ArgumentException($"{nameof(chainSpec.Parameters.Sip4844TransitionTimestamp)} should be set in order to use {nameof(_chainSpec.Parameters.BlobSchedule)}");

                foreach (BlobScheduleSettings settings in chainSpec.Parameters.BlobSchedule)
                {
                    if (settings.Timestamp <= genesisTimestamp)
                    {
                        continue;
                    }

                    if (settings.Timestamp < sip4844Timestamp)
                    {
                        throw new ArgumentException($"Blob settings are scheduled at {settings.Timestamp}, before SIP-4844, activated at {chainSpec.Parameters.Sip4844TransitionTimestamp}");
                    }

                    if (settings.Target > settings.Max)
                    {
                        throw new ArgumentException($"Blob schedule target ({settings.Target}) should not exceed max ({settings.Max}).");
                    }

                    transitions.Add(settings.Timestamp);
                }
            }

            (ForkActivation Activation, IReleaseSpec Spec)[] allTransitions = CreateTransitions(_chainSpec, transitionBlockNumbers, transitionTimestamps);

            LoadTransitions(allTransitions);

            TransitionActivations = CreateTransitionActivations(transitionBlockNumbers, transitionTimestamps);
            _forkAware = ForkAwareForChain(_chainSpec.ChainId);

            if (_chainSpec.Parameters.TerminalPoWBlockNumber is not null)
            {
                MergeBlockNumber = (ForkActivation)(_chainSpec.Parameters.TerminalPoWBlockNumber.Value + 1);
            }

            TerminalTotalDifficulty = _chainSpec.Parameters.TerminalTotalDifficulty;
        }

        private static readonly List<IForkAwareSpecProvider> _knownProviders =
        [
            MainnetSpecProvider.Instance,
            GnosisSpecProvider.Instance,
            ChiadoSpecProvider.Instance,
            SepoliaSpecProvider.Instance,
            HoodiSpecProvider.Instance,
            MordenSpecProvider.Instance,
        ];
        private static FrozenDictionary<ulong, IForkAwareSpecProvider>? _knownProvidersByChainId;

        /// <summary>
        /// Built-in plus plugin-registered <see cref="IForkAwareSpecProvider"/>s, keyed by chain id.
        /// The dictionary is rebuilt lazily after each <see cref="RegisterProvider"/> call.
        /// </summary>
        /// <remarks>Plugin registration is expected at startup only; not safe for concurrent mutation.</remarks>
        public static FrozenDictionary<ulong, IForkAwareSpecProvider> KnownProvidersByChainId =>
            _knownProvidersByChainId ??= _knownProviders.ToFrozenDictionary(static p => p.ChainId);

        /// <summary>
        /// Registers an additional <see cref="IForkAwareSpecProvider"/> (e.g. from a plugin) so that
        /// <see cref="ChainSpecBasedSpecProvider"/> can resolve forks for its chain id. Call at startup.
        /// </summary>
        public static void RegisterProvider(IForkAwareSpecProvider provider)
        {
            _knownProviders.Add(provider);
            _knownProvidersByChainId = null;
        }

        private static IForkAwareSpecProvider? ForkAwareForChain(ulong chainId) =>
            KnownProvidersByChainId.GetValueOrDefault(chainId);

        private (ForkActivation, IReleaseSpec Spec)[] CreateTransitions(
            ChainSpec chainSpec,
            SortedSet<ulong> transitionBlockNumbers,
            SortedSet<ulong> transitionTimestamps)
        {
            (ForkActivation Activation, IReleaseSpec Spec)[] transitions = new (ForkActivation, IReleaseSpec Spec)[transitionBlockNumbers.Count + transitionTimestamps.Count];
            ulong biggestBlockTransition = transitionBlockNumbers.Max;

            int index = 0;
            foreach (ulong releaseStartBlock in transitionBlockNumbers)
            {
                IReleaseSpec releaseSpec = CreateReleaseSpec(chainSpec, releaseStartBlock, chainSpec.Genesis?.Timestamp ?? 0);
                transitions[index++] = ((ForkActivation)releaseStartBlock, releaseSpec);
            }

            foreach (ulong releaseStartTimestamp in transitionTimestamps)
            {
                ForkActivation forkActivation = (biggestBlockTransition, releaseStartTimestamp);
                IReleaseSpec releaseSpec = CreateReleaseSpec(chainSpec, biggestBlockTransition, releaseStartTimestamp);
                transitions[index++] = (forkActivation, releaseSpec);
            }

            return transitions;
        }

        private static ForkActivation[] CreateTransitionActivations(SortedSet<ulong> transitionBlockNumbers, SortedSet<ulong> transitionTimestamps)
        {
            ulong biggestBlockTransition = transitionBlockNumbers.Max;

            ForkActivation[] transitionActivations = new ForkActivation[transitionBlockNumbers.Count - 1 + transitionTimestamps.Count];

            int index = 0;
            foreach (ulong blockNumber in transitionBlockNumbers.Skip(1))
            {
                transitionActivations[index++] = new ForkActivation(blockNumber);
            }

            foreach (ulong timestamp in transitionTimestamps)
            {
                transitionActivations[index++] = new ForkActivation(biggestBlockTransition, timestamp);
            }

            return transitionActivations;
        }

        private static ulong BlockOf(ulong? transition, ulong nullSentinel = ulong.MaxValue) =>
            transition ?? nullSentinel;

        protected virtual ReleaseSpec CreateReleaseSpec(ChainSpec chainSpec, ulong releaseStartBlock, ulong? releaseStartTimestamp = null)
        {
            ulong block = releaseStartBlock;
            ReleaseSpec releaseSpec = CreateEmptyReleaseSpec();
            releaseSpec.MaximumUncleCount = IsPostMergeRelease(chainSpec, releaseStartBlock, releaseStartTimestamp) ? 0 : 2;
            releaseSpec.DifficultyBoundDivisor = 1;
            releaseSpec.IsTimeAdjustmentPostOlympic = true; // TODO: this is Duration, review
            releaseSpec.MaximumExtraDataSize = chainSpec.Parameters.MaximumExtraDataSize;
            releaseSpec.MinGasLimit = chainSpec.Parameters.MinGasLimit;
            releaseSpec.MinHistoryRetentionEpochs = chainSpec.Parameters.MinHistoryRetentionEpochs;
            releaseSpec.MinBalRetentionEpochs = chainSpec.Parameters.MinBalRetentionEpochs;
            releaseSpec.GasLimitBoundDivisor = chainSpec.Parameters.GasLimitBoundDivisor;
            releaseSpec.IsSip170Enabled = BlockOf(chainSpec.Parameters.MaxCodeSizeTransition) <= block ||
                                          (chainSpec.Parameters.MaxCodeSizeTransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.MaxCodeSize = releaseSpec.IsSip170Enabled ? (chainSpec.Parameters.MaxCodeSize ?? long.MaxValue) : long.MaxValue;
            releaseSpec.IsSip2Enabled = true;
            releaseSpec.IsSip100Enabled = true;
            releaseSpec.IsSip7Enabled = BlockOf(chainSpec.Parameters.Sip7Transition, 0) <= block;
            releaseSpec.IsSip140Enabled = BlockOf(chainSpec.Parameters.Sip140Transition, 0) <= block;
            releaseSpec.IsSip145Enabled = BlockOf(chainSpec.Parameters.Sip145Transition, 0) <= block;
            releaseSpec.IsSip150Enabled = BlockOf(chainSpec.Parameters.Sip150Transition, 0) <= block;
            releaseSpec.IsSip152Enabled = BlockOf(chainSpec.Parameters.Sip152Transition) <= block;
            releaseSpec.IsSip155Enabled = BlockOf(chainSpec.Parameters.Sip155Transition, 0) <= block;
            releaseSpec.IsSip160Enabled = BlockOf(chainSpec.Parameters.Sip160Transition, 0) <= block;
            releaseSpec.IsSip158Enabled = BlockOf(chainSpec.Parameters.Sip161abcTransition, 0) <= block;
            releaseSpec.IsSip196Enabled = (chainSpec.ByzantiumBlockNumber ?? 0UL) <= block;
            releaseSpec.IsSip197Enabled = (chainSpec.ByzantiumBlockNumber ?? 0UL) <= block;
            releaseSpec.IsSip198Enabled = (chainSpec.ByzantiumBlockNumber ?? 0UL) <= block;
            releaseSpec.IsSip211Enabled = BlockOf(chainSpec.Parameters.Sip211Transition, 0) <= block;
            releaseSpec.IsSip214Enabled = BlockOf(chainSpec.Parameters.Sip214Transition, 0) <= block;
            releaseSpec.IsSip658Enabled = BlockOf(chainSpec.Parameters.Sip658Transition, 0) <= block;
            releaseSpec.IsSip649Enabled = (chainSpec.ByzantiumBlockNumber ?? 0UL) <= block;
            releaseSpec.IsSip1014Enabled = BlockOf(chainSpec.Parameters.Sip1014Transition, 0) <= block;
            releaseSpec.IsSip1052Enabled = BlockOf(chainSpec.Parameters.Sip1052Transition, 0) <= block;
            releaseSpec.IsSip1108Enabled = BlockOf(chainSpec.Parameters.Sip1108Transition) <= block;
            releaseSpec.IsSip1234Enabled = (chainSpec.ConstantinopleBlockNumber ?? chainSpec.ConstantinopleFixBlockNumber ?? 0UL) <= block;
            releaseSpec.IsSip1283Enabled = BlockOf(chainSpec.Parameters.Sip1283Transition) <= block
                && (BlockOf(chainSpec.Parameters.Sip1283DisableTransition) > block
                    || BlockOf(chainSpec.Parameters.Sip1283ReenableTransition) <= block);
            releaseSpec.IsSip1344Enabled = BlockOf(chainSpec.Parameters.Sip1344Transition) <= block;
            releaseSpec.IsSip1884Enabled = BlockOf(chainSpec.Parameters.Sip1884Transition) <= block;
            releaseSpec.IsSip2028Enabled = BlockOf(chainSpec.Parameters.Sip2028Transition) <= block;
            releaseSpec.IsSip2200Enabled = BlockOf(chainSpec.Parameters.Sip2200Transition) <= block
                || BlockOf(chainSpec.Parameters.Sip1706Transition) <= block && releaseSpec.IsSip1283Enabled;
            releaseSpec.IsSip1559Enabled = BlockOf(chainSpec.Parameters.Sip1559Transition) <= block;
            releaseSpec.Sip1559TransitionBlock = BlockOf(chainSpec.Parameters.Sip1559Transition);
            releaseSpec.IsSip2537Enabled = BlockOf(chainSpec.Parameters.Sip2537Transition) <= block ||
                                           (chainSpec.Parameters.Sip2537TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip2565Enabled = BlockOf(chainSpec.Parameters.Sip2565Transition) <= block;
            releaseSpec.IsSip2929Enabled = BlockOf(chainSpec.Parameters.Sip2929Transition) <= block;
            releaseSpec.IsSip2930Enabled = BlockOf(chainSpec.Parameters.Sip2930Transition) <= block;
            releaseSpec.IsSip3198Enabled = BlockOf(chainSpec.Parameters.Sip3198Transition) <= block;
            releaseSpec.IsSip3541Enabled = BlockOf(chainSpec.Parameters.Sip3541Transition) <= block;
            releaseSpec.IsSip3529Enabled = BlockOf(chainSpec.Parameters.Sip3529Transition) <= block;
            releaseSpec.IsSip3607Enabled = BlockOf(chainSpec.Parameters.Sip3607Transition) <= block;
            releaseSpec.ValidateChainId = BlockOf(chainSpec.Parameters.ValidateChainIdTransition, 0) <= block;
            releaseSpec.ValidateReceipts = ((chainSpec.Parameters.ValidateReceiptsTransition > 0)
                ? Math.Max(BlockOf(chainSpec.Parameters.ValidateReceiptsTransition, 0),
                           BlockOf(chainSpec.Parameters.Sip658Transition, 0))
                : 0UL) <= block;
            releaseSpec.Sip1559BaseFeeMinValue = releaseSpec.IsSip1559Enabled
                && BlockOf(chainSpec.Parameters.Sip1559BaseFeeMinValueTransition) <= block
                    ? chainSpec.Parameters.Sip1559BaseFeeMinValue
                    : null;
            releaseSpec.ElasticityMultiplier = chainSpec.Parameters.Sip1559ElasticityMultiplier ?? Sip1559Constants.DefaultElasticityMultiplier;
            releaseSpec.ForkBaseFee = chainSpec.Parameters.Sip1559BaseFeeInitialValue ?? Sip1559Constants.DefaultForkBaseFee;
            releaseSpec.BaseFeeMaxChangeDenominator = chainSpec.Parameters.Sip1559BaseFeeMaxChangeDenominator ?? Sip1559Constants.DefaultBaseFeeMaxChangeDenominator;

            releaseSpec.IsSip1153Enabled = BlockOf(chainSpec.Parameters.Sip1153Transition) <= block ||
                                          (chainSpec.Parameters.Sip1153TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip3651Enabled = BlockOf(chainSpec.Parameters.Sip3651Transition) <= block ||
                                          (chainSpec.Parameters.Sip3651TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip3855Enabled = BlockOf(chainSpec.Parameters.Sip3855Transition) <= block ||
                                           (chainSpec.Parameters.Sip3855TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip3860Enabled = BlockOf(chainSpec.Parameters.Sip3860Transition) <= block ||
                                           (chainSpec.Parameters.Sip3860TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip4895Enabled = (chainSpec.Parameters.Sip4895TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.WithdrawalTimestamp = chainSpec.Parameters.Sip4895TransitionTimestamp ?? ulong.MaxValue;

            releaseSpec.IsSip4844Enabled = BlockOf(chainSpec.Parameters.Sip4844Transition) <= block ||
                (chainSpec.Parameters.Sip4844TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip7951Enabled = (chainSpec.Parameters.Sip7951TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsRip7212Enabled = (chainSpec.Parameters.Rip7212TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.Sip4844TransitionTimestamp = chainSpec.Parameters.Sip4844TransitionTimestamp ?? ulong.MaxValue;
            releaseSpec.IsSip5656Enabled = BlockOf(chainSpec.Parameters.Sip5656Transition) <= block ||
                                           (chainSpec.Parameters.Sip5656TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip6780Enabled = BlockOf(chainSpec.Parameters.Sip6780Transition) <= block ||
                                           (chainSpec.Parameters.Sip6780TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip4788Enabled = (chainSpec.Parameters.Sip4788TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.Sip4788ContractAddress = chainSpec.Parameters.Sip4788ContractAddress;
            releaseSpec.IsSip2935Enabled = BlockOf(chainSpec.Parameters.Sip2935Transition) <= block ||
                                           (chainSpec.Parameters.Sip2935TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.Sip2935ContractAddress = chainSpec.Parameters.Sip2935ContractAddress;
            releaseSpec.Sip2935RingBufferSize = chainSpec.Parameters.Sip2935RingBufferSize;

            releaseSpec.IsSip7702Enabled = BlockOf(chainSpec.Parameters.Sip7702Transition) <= block ||
                                           (chainSpec.Parameters.Sip7702TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip7823Enabled = (chainSpec.Parameters.Sip7823TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;

            releaseSpec.IsSip6110Enabled = (chainSpec.Parameters.Sip6110TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.DepositContractAddress = chainSpec.Parameters.DepositContractAddress;

            releaseSpec.IsSip7002Enabled = (chainSpec.Parameters.Sip7002TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.Sip7002ContractAddress = chainSpec.Parameters.Sip7002ContractAddress;

            releaseSpec.IsSip7251Enabled = (chainSpec.Parameters.Sip7251TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.Sip7251ContractAddress = chainSpec.Parameters.Sip7251ContractAddress;
            releaseSpec.IsSip7623Enabled = BlockOf(chainSpec.Parameters.Sip7623Transition) <= block ||
                                           (chainSpec.Parameters.Sip7623TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip7976Enabled = (chainSpec.Parameters.Sip7976TransitionTimestamp ?? chainSpec.AmsterdamTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip7981Enabled = (chainSpec.Parameters.Sip7981TransitionTimestamp ?? chainSpec.AmsterdamTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip7883Enabled = (chainSpec.Parameters.Sip7883TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;

            releaseSpec.IsSip7594Enabled = (chainSpec.Parameters.Sip7594TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip7825Enabled = (chainSpec.Parameters.Sip7825TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip7918Enabled = (chainSpec.Parameters.Sip7918TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip8024Enabled = (chainSpec.Parameters.Sip8024TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip8246Enabled = (chainSpec.Parameters.Sip8246TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip8038Enabled = (chainSpec.Parameters.Sip8038TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip8282Enabled = (chainSpec.Parameters.Sip8282TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;

            bool sip1559FeeCollector = releaseSpec.IsSip1559Enabled && BlockOf(chainSpec.Parameters.Sip1559FeeCollectorTransition) <= block;
            bool sip4844FeeCollector = releaseSpec.IsSip4844Enabled && (chainSpec.Parameters.Sip4844FeeCollectorTransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.FeeCollector = (sip1559FeeCollector || sip4844FeeCollector) ? chainSpec.Parameters.FeeCollector : null;
            releaseSpec.IsSip4844FeeCollectorEnabled = sip4844FeeCollector;

            releaseSpec.IsSip7934Enabled = (chainSpec.Parameters.Sip7934TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.Sip7934MaxRlpBlockSize = chainSpec.Parameters.Sip7934MaxRlpBlockSize;

            releaseSpec.IsSip7939Enabled = (chainSpec.Parameters.Sip7939TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;

            releaseSpec.IsSip8037Enabled = (chainSpec.Parameters.Sip8037TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip7778Enabled = (chainSpec.Parameters.Sip7778TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;

            releaseSpec.IsSip7928Enabled = (chainSpec.Parameters.Sip7928TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;
            releaseSpec.IsSip7843Enabled = (chainSpec.Parameters.Sip7843TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;

            releaseSpec.IsSip7708Enabled = (chainSpec.Parameters.Sip7708TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;

            releaseSpec.IsSip7954Enabled = (chainSpec.Parameters.Sip7954TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;

            if (releaseSpec.IsSip7954Enabled)
            {
                releaseSpec.MaxCodeSize = CodeSizeConstants.MaxCodeSizeSip7954;
            }

            releaseSpec.IsSip2780Enabled = (chainSpec.Parameters.Sip2780TransitionTimestamp ?? ulong.MaxValue) <= releaseStartTimestamp;

            foreach (IChainSpecEngineParameters item in _chainSpec.EngineChainSpecParametersProvider
                         .AllChainSpecParameters)
            {
                item.ApplyToReleaseSpec(releaseSpec, block, releaseStartTimestamp);
            }

            SetBlobScheduleParameters();

            return releaseSpec;

            void SetBlobScheduleParameters()
            {
                if (releaseSpec.Sip4844TransitionTimestamp > releaseStartTimestamp)
                {
                    return;
                }

                BlobScheduleSettings? blobSchedule = chainSpec.Parameters.BlobSchedule?.OrderByDescending(bs => bs).FirstOrDefault(bs => bs.Timestamp <= releaseStartTimestamp);

                if (blobSchedule is not null)
                {
                    releaseSpec.TargetBlobCount = blobSchedule.Target;
                    releaseSpec.MaxBlobCount = blobSchedule.Max;
                    releaseSpec.BlobBaseFeeUpdateFraction = blobSchedule.BaseFeeUpdateFraction;
                }
                else if (releaseSpec.Sip4844TransitionTimestamp <= releaseStartTimestamp)
                {
                    releaseSpec.TargetBlobCount = Sip4844Constants.DefaultTargetBlobCount;
                    releaseSpec.MaxBlobCount = Sip4844Constants.DefaultMaxBlobCount;
                    releaseSpec.BlobBaseFeeUpdateFraction = Sip4844Constants.DefaultBlobGasPriceUpdateFraction;
                }
            }
        }

        private static bool IsPostMergeRelease(ChainSpec chainSpec, ulong releaseStartBlock, ulong? releaseStartTimestamp) =>
            chainSpec.Parameters.TerminalTotalDifficulty?.IsZero == true
            || releaseStartBlock > (chainSpec.Parameters.TerminalPoWBlockNumber ?? ulong.MaxValue)
            || (chainSpec.Parameters.Sip4895TransitionTimestamp ?? ulong.MaxValue) <= (releaseStartTimestamp ?? 0);

        public void UpdateMergeTransitionInfo(ulong? blockNumber, UInt256? terminalTotalDifficulty = null)
        {
            if (blockNumber is not null)
            {
                MergeBlockNumber = (ForkActivation)blockNumber;
            }

            if (terminalTotalDifficulty is not null)
            {
                TerminalTotalDifficulty = terminalTotalDifficulty;
            }
        }

        public ForkActivation? MergeBlockNumber { get; private set; }
        public ulong TimestampFork { get; private set; }

        public UInt256? TerminalTotalDifficulty { get; private set; }

        public ulong? DaoBlockNumber => _chainSpec.DaoForkBlockNumber;
        public ulong? BeaconChainGenesisTimestamp => _chainSpec.Parameters.BeaconChainGenesisTimestamp;

        public ulong NetworkId => _chainSpec.NetworkId;
        public ulong ChainId => _chainSpec.ChainId;
        public string SealEngine => _chainSpec.SealEngineType;
        public IEnumerable<string> AvailableForks => _forkAware?.AvailableForks ?? [];

        public bool TryGetForkSpec(string forkName, out IReleaseSpec? spec)
        {
            spec = null;
            return _forkAware?.TryGetForkSpec(forkName, out spec) ?? false;
        }
    }
}
