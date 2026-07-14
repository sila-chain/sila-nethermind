// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Autofac;
using Autofac.Core;
using Nethermind.Api.Extensions;
using Nethermind.Consensus.Rewards;
using Nethermind.Core;
using Nethermind.Specs.ChainSpecStyle;

namespace Nethermind.Consensus.Silash
{
    public class SilashPlugin(ChainSpec chainSpec, IMiningConfig miningConfig) : IConsensusPlugin
    {
        public string Name => SealEngineType;

        public string Description => $"{SealEngineType} Consensus";

        public string Author => "Nethermind";

        public bool Enabled => chainSpec.SealEngineType == SealEngineType;


        public string SealEngineType => Core.SealEngineType.Silash;

        public IModule Module => new SilHashModule(miningConfig);
    }

    public class SilHashModule(IMiningConfig miningConfig) : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder
                .AddSingleton<IRewardCalculatorSource, RewardCalculator>()
                .AddSingleton<IDifficultyCalculator, SilashDifficultyCalculator>()
                .AddSingleton<ISilash, Silash>()
                .AddSingleton<ISealValidator, SilashSealValidator>()

                .AddSingleton<SilashBlockProducerFactory>()
                .Bind<IBlockProducerFactory, SilashBlockProducerFactory>()
                .Bind<IBlockProducerRunnerFactory, SilashBlockProducerFactory>()
                ;

            if (miningConfig.Enabled)
            {
                builder.AddSingleton<ISealer, SilashSealer>();
            }
        }
    }
}
