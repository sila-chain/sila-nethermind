// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Blockchain;
using Nethermind.Config;
using Nethermind.Consensus.Processing;
using Nethermind.Consensus.Producers;
using Nethermind.Consensus.Transactions;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Savm.State;
using Nethermind.Logging;

namespace Nethermind.Consensus.Silash
{
    public class MinedBlockProducer(ITxSource txSource,
        IBlockchainProcessor processor,
        ISealer sealer,
        IBlockTree blockTree,
        IWorldState stateProvider,
        IGasLimitCalculator gasLimitCalculator,
        ITimestamper timestamper,
        ISpecProvider specProvider,
        ILogManager logManager,
        IBlocksConfig blocksConfig) : BlockProducerBase(
            txSource,
            processor,
            sealer,
            blockTree,
            stateProvider,
            gasLimitCalculator,
            timestamper,
            specProvider,
            logManager,
            new SilashDifficultyCalculator(specProvider),
            blocksConfig)
    {
    }
}
