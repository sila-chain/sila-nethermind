// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Reflection;
using Nethermind.Consensus.Silash;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;
using Nethermind.Specs;
using Nethermind.Specs.ChainSpecStyle;
using Nethermind.Specs.Test;
using Nethermind.Specs.Test.ChainSpecStyle;
using NUnit.Framework;

namespace Nethermind.Silash.Test;

public class ChainSpecTest
{
    [Test]
    public void Bound_divisors_set_correctly()
    {
        ChainSpec chainSpec = new()
        {
            Parameters = new ChainParameters { GasLimitBoundDivisor = 17UL }
        };

        chainSpec.EngineChainSpecParametersProvider =
            new TestChainSpecParametersProvider(new SilashChainSpecEngineParameters { DifficultyBoundDivisor = 19 });


        ChainSpecBasedSpecProvider provider = new(chainSpec);
        Assert.That(provider.GenesisSpec.DifficultyBoundDivisor, Is.EqualTo(19));
        Assert.That(provider.GenesisSpec.GasLimitBoundDivisor, Is.EqualTo(17UL));
    }

    [Test]
    public void Difficulty_bomb_delays_loaded_correctly()
    {
        ChainSpec chainSpec = new()
        {
            Parameters = new ChainParameters(),
        };
        chainSpec.EngineChainSpecParametersProvider = new TestChainSpecParametersProvider(
            new SilashChainSpecEngineParameters
            {
                DifficultyBombDelays = new Dictionary<ulong, ulong>
                {
                    { 3UL, 100UL },
                    { 7UL, 200UL },
                    { 13UL, 300UL },
                    { 17UL, 400UL },
                    { 19UL, 500UL },
                }
            });

        ChainSpecBasedSpecProvider provider = new(chainSpec);
        Assert.That(provider.GetSpec((ForkActivation)3).DifficultyBombDelay, Is.EqualTo(100));
        Assert.That(provider.GetSpec((ForkActivation)7).DifficultyBombDelay, Is.EqualTo(300));
        Assert.That(provider.GetSpec((ForkActivation)13).DifficultyBombDelay, Is.EqualTo(600));
        Assert.That(provider.GetSpec((ForkActivation)17).DifficultyBombDelay, Is.EqualTo(1000));
        Assert.That(provider.GetSpec((ForkActivation)19).DifficultyBombDelay, Is.EqualTo(1500));
    }

    [Test]
    public void Sip_transitions_loaded_correctly()
    {
        const long maxCodeTransition = 1;
        const long maxCodeSize = 1;

        ChainSpec chainSpec = new()
        {
            ByzantiumBlockNumber = 1960,
            ConstantinopleBlockNumber = 6490,
            Parameters = new ChainParameters
            {
                MaxCodeSizeTransition = maxCodeTransition,
                MaxCodeSize = maxCodeSize,
                Registrar = Address.Zero,
                MinGasLimit = 11UL,
                MinHistoryRetentionEpochs = 11,
                MinBalRetentionEpochs = 7,
                GasLimitBoundDivisor = 13UL,
                MaximumExtraDataSize = 17,
                Sip140Transition = 1400L,
                Sip145Transition = 1450L,
                Sip150Transition = 1500L,
                Sip152Transition = 1520L,
                Sip155Transition = 1550L,
                Sip160Transition = 1600L,
                Sip161abcTransition = 1580L,
                Sip161dTransition = 1580L,
                Sip211Transition = 2110L,
                Sip214Transition = 2140L,
                Sip658Transition = 6580L,
                Sip1014Transition = 10140L,
                Sip1052Transition = 10520L,
                Sip1108Transition = 11080L,
                Sip1283Transition = 12830L,
                Sip1283DisableTransition = 12831L,
                Sip1344Transition = 13440L,
                Sip1884Transition = 18840L,
                Sip2028Transition = 20280L,
                Sip2200Transition = 22000L,
                Sip2315Transition = 23150L,
                Sip2565Transition = 25650L,
                Sip2929Transition = 29290L,
                Sip2930Transition = 29300L,
                Sip1559Transition = 15590L,
                Sip1559FeeCollectorTransition = 15591L,
                FeeCollector = Address.SystemUser,
                Sip1559BaseFeeMinValueTransition = 15592L,
                Sip1559BaseFeeMinValue = UInt256.UInt128MaxValue,
                Sip3198Transition = 31980L,
                Sip3529Transition = 35290L,
                Sip3541Transition = 35410L,
                Sip1283ReenableTransition = 23000L,
                ValidateChainIdTransition = 24000L,
                ValidateReceiptsTransition = 24000L,
                MergeForkIdTransition = 40000L,
                Sip3651TransitionTimestamp = 1000000012,
                Sip3855TransitionTimestamp = 1000000012,
                Sip3860TransitionTimestamp = 1000000012,
                Sip1153TransitionTimestamp = 1000000024,
                Sip2537TransitionTimestamp = 1000000024,

                Sip7702TransitionTimestamp = 1000000032,
            }
        };
        chainSpec.EngineChainSpecParametersProvider = new TestChainSpecParametersProvider(
            new SilashChainSpecEngineParameters
            {
                HomesteadTransition = 70,
                Sip100bTransition = 1000
            });


        ChainSpecBasedSpecProvider provider = new(chainSpec);
        Assert.That(provider.GetSpec((ForkActivation)(maxCodeTransition - 1)).MaxCodeSize, Is.EqualTo(long.MaxValue), "one before");
        Assert.That(provider.GetSpec((ForkActivation)maxCodeTransition).MaxCodeSize, Is.EqualTo(maxCodeSize), "at transition");
        Assert.That(provider.GetSpec((ForkActivation)(maxCodeTransition + 1)).MaxCodeSize, Is.EqualTo(maxCodeSize), "one after");

        OverridableReleaseSpec expected = new(new ReleaseSpec());

        void TestTransitions(ForkActivation activation, Action<OverridableReleaseSpec> changes)
        {
            changes(expected);
            IReleaseSpec underTest = provider.GetSpec(activation);
            AssertReleaseSpecEquivalent(underTest, expected);
        }

        TestTransitions((ForkActivation)0L, r =>
        {
            r.DifficultyBoundDivisor = 0x800;
            r.MinGasLimit = 11UL;
            r.MinHistoryRetentionEpochs = 11L;
            r.MinBalRetentionEpochs = 7L;
            r.GasLimitBoundDivisor = 13UL;
            r.MaximumExtraDataSize = 17L;
            r.MaxCodeSize = long.MaxValue;
            r.Sip1559TransitionBlock = 15590L;
            r.IsTimeAdjustmentPostOlympic = true;
            r.MaximumUncleCount = 2;
            r.WithdrawalTimestamp = ulong.MaxValue;
            r.Sip4844TransitionTimestamp = ulong.MaxValue;
        });

        TestTransitions((ForkActivation)1L, r =>
        {
            r.MaxCodeSize = maxCodeSize;
            r.IsSip170Enabled = true;
        });
        TestTransitions((ForkActivation)70L, r => { r.IsSip2Enabled = r.IsSip7Enabled = true; });
        TestTransitions((ForkActivation)1000L, r => { r.IsSip100Enabled = true; });
        TestTransitions((ForkActivation)1400L, r => { r.IsSip140Enabled = true; });
        TestTransitions((ForkActivation)1450L, r => { r.IsSip145Enabled = true; });
        TestTransitions((ForkActivation)1500L, r => { r.IsSip150Enabled = true; });
        TestTransitions((ForkActivation)1520L, r => { r.IsSip152Enabled = true; });
        TestTransitions((ForkActivation)1550L, r => { r.IsSip155Enabled = true; });
        TestTransitions((ForkActivation)1580L, r => { r.IsSip158Enabled = true; });
        TestTransitions((ForkActivation)1600L, r => { r.IsSip160Enabled = true; });
        TestTransitions((ForkActivation)1960L,
            r => { r.IsSip196Enabled = r.IsSip197Enabled = r.IsSip198Enabled = r.IsSip649Enabled = true; });
        TestTransitions((ForkActivation)2110L, r => { r.IsSip211Enabled = true; });
        TestTransitions((ForkActivation)2140L, r => { r.IsSip214Enabled = true; });
        TestTransitions((ForkActivation)6580L, r => { r.IsSip658Enabled = r.IsSip1234Enabled = true; });
        TestTransitions((ForkActivation)10140L, r => { r.IsSip1014Enabled = true; });
        TestTransitions((ForkActivation)10520L, r => { r.IsSip1052Enabled = true; });
        TestTransitions((ForkActivation)11180L, r => { r.IsSip1108Enabled = true; });
        TestTransitions((ForkActivation)12830L, r => { r.IsSip1283Enabled = true; });
        TestTransitions((ForkActivation)12831L, r => { r.IsSip1283Enabled = false; });
        TestTransitions((ForkActivation)13440L, r => { r.IsSip1344Enabled = true; });
        TestTransitions((ForkActivation)15590L, r => { r.IsSip1559Enabled = true; });
        TestTransitions((ForkActivation)15591L, r => { r.FeeCollector = Address.SystemUser; });
        TestTransitions((ForkActivation)15592L, r => { r.Sip1559BaseFeeMinValue = UInt256.UInt128MaxValue; });
        TestTransitions((ForkActivation)18840L, r => { r.IsSip1884Enabled = true; });
        TestTransitions((ForkActivation)20280L, r => { r.IsSip2028Enabled = true; });
        TestTransitions((ForkActivation)22000L, r => { r.IsSip2200Enabled = true; });
        TestTransitions((ForkActivation)23000L, r => { r.IsSip1283Enabled = r.IsSip1344Enabled = true; });
        TestTransitions((ForkActivation)24000L, r => { r.ValidateChainId = r.ValidateReceipts = true; });
        TestTransitions((ForkActivation)29290L, r => { r.IsSip2929Enabled = r.IsSip2565Enabled = true; });
        TestTransitions((ForkActivation)29300L, r => { r.IsSip2930Enabled = true; });
        TestTransitions((ForkActivation)31980L, r => { r.IsSip3198Enabled = true; });
        TestTransitions((ForkActivation)35290L, r => { r.IsSip3529Enabled = true; });
        TestTransitions((ForkActivation)35410L, r => { r.IsSip3541Enabled = true; });
        TestTransitions((ForkActivation)35410L, r => { r.IsSip3541Enabled = true; });


        TestTransitions((41000L, 1000000012), r =>
        {
            r.IsSip3651Enabled = true;
            r.IsSip3855Enabled = true;
            r.IsSip3860Enabled = true;
        });
        TestTransitions((40001L, 1000000024), r => { r.IsSip1153Enabled = r.IsSip2537Enabled = true; });
        TestTransitions((40001L, 1000000032), r => { r.IsSip7702Enabled = true; });
    }

    private static void AssertReleaseSpecEquivalent(IReleaseSpec actual, IReleaseSpec expected) =>
        Assert.Multiple(() =>
        {
            foreach (PropertyInfo property in typeof(IReleaseSpec).GetProperties())
            {
                if (property.Name is nameof(IReleaseSpec.Name) or nameof(IReleaseSpec.Precompiles))
                {
                    continue;
                }

                Assert.That(property.GetValue(actual), Is.EqualTo(property.GetValue(expected)), property.Name);
            }
        });
}
