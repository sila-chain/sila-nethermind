// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Blockchain;
using Nethermind.Blockchain.Receipts;
using Nethermind.Config;
using Nethermind.Core.Specs;
using Nethermind.Db.LogIndex;
using Nethermind.Facade;
using Nethermind.Facade.Sil;
using Nethermind.JsonRpc.Modules.Sil.GasPrice;
using Nethermind.JsonRpc.Modules.Sil.FeeHistory;
using Nethermind.Logging;
using Nethermind.Network;
using Nethermind.State;
using Nethermind.TxPool;
using Nethermind.Wallet;

namespace Nethermind.JsonRpc.Modules.Sil
{
    public class SilModuleFactory(
        ITxPool txPool,
        ITxSender txSender,
        IWallet wallet,
        IBlockTree blockTree,
        IJsonRpcConfig config,
        ILogManager logManager,
        IStateReader stateReader,
        IBlockchainBridgeFactory blockchainBridgeFactory,
        ISpecProvider specProvider,
        IReceiptStorage receiptStorage,
        IGasPriceOracle gasPriceOracle,
        ISilSyncingInfo silSyncingInfo,
        IFeeHistoryOracle feeHistoryOracle,
        IProtocolsManager protocolsManager,
        IBlocksConfig blocksConfig,
        IForkInfo forkInfo,
        ILogIndexConfig logIndexConfig,
        IReceiptConfig receiptConfig,
        ISilCapabilitiesProvider capabilitiesProvider,
        IBlockForRpcFactory blockForRpcFactory)
        : ModuleFactoryBase<ISilRpcModule>
    {
        private readonly ulong _secondsPerSlot = blocksConfig.SecondsPerSlot;
        private readonly IReadOnlyBlockTree _blockTree = blockTree.AsReadOnly();
        private readonly HeadBlockSignal _headBlockSignal = new(blockTree);

        public override ISilRpcModule Create() => new SilRpcModule(
                config,
                blockchainBridgeFactory.CreateBlockchainBridge(),
                _blockTree,
                blockTree,
                receiptStorage,
                stateReader,
                txPool,
                txSender,
                wallet,
                logManager,
                specProvider,
                gasPriceOracle,
                silSyncingInfo,
                feeHistoryOracle,
                protocolsManager,
                forkInfo,
                logIndexConfig,
                receiptConfig,
                _secondsPerSlot,
                _headBlockSignal,
                capabilitiesProvider,
                blockForRpcFactory);
    }
}
