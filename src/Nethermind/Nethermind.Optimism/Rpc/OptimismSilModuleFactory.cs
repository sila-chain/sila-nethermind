// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Blockchain;
using Nethermind.Blockchain.Receipts;
using Nethermind.Core.Specs;
using Nethermind.Facade;
using Nethermind.Facade.Sil;
using Nethermind.JsonRpc.Modules.Sil;
using Nethermind.JsonRpc.Modules.Sil.GasPrice;
using Nethermind.JsonRpc.Modules.Sil.FeeHistory;
using Nethermind.Logging;
using Nethermind.TxPool;
using Nethermind.Wallet;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;
using Nethermind.Blockchain.Find;
using Nethermind.Config;
using Nethermind.Core;
using Nethermind.Crypto;
using Nethermind.Db.LogIndex;
using Nethermind.JsonRpc.Client;
using Nethermind.Network;
using Nethermind.Serialization.Json;
using Nethermind.State;

namespace Nethermind.Optimism.Rpc;

public class OptimismEthModuleFactory : ModuleFactoryBase<IOptimismEthRpcModule>
{
    private readonly ILogManager _logManager;
    private readonly IStateReader _stateReader;
    private readonly IBlockchainBridgeFactory _blockchainBridgeFactory;
    private readonly ITxPool _txPool;
    private readonly ITxSender _txSender;
    private readonly IWallet _wallet;
    private readonly IJsonRpcConfig _rpcConfig;
    private readonly ISpecProvider _specProvider;
    private readonly IGasPriceOracle _gasPriceOracle;
    private readonly ISilSyncingInfo _ethSyncingInfo;
    private readonly IFeeHistoryOracle _feeHistoryOracle;
    private readonly ISilaEcdsa _ecdsa;
    private readonly ITxSealer _sealer;
    private readonly IBlockFinder _blockFinder;
    private readonly IBlockTree _blockTree;
    private readonly IReceiptFinder _receiptFinder;
    private readonly ISilCapabilitiesProvider _capabilitiesProvider;
    private readonly IOptimismSpecHelper _opSpecHelper;
    private readonly IProtocolsManager _protocolsManager;
    private readonly IForkInfo _forkInfo;
    private readonly ILogIndexConfig _logIndexConfig;
    private readonly IReceiptConfig _receiptConfig;
    private readonly ulong? _secondsPerSlot;
    private readonly IJsonRpcClient? _sequencerRpcClient;
    private readonly HeadBlockSignal _headBlockSignal;
    private readonly IBlockForRpcFactory _blockForRpcFactory;

    public OptimismEthModuleFactory(IJsonRpcConfig rpcConfig,
        IBlockchainBridgeFactory blockchainBridgeFactory,
        IBlockFinder blockFinder,
        IBlockTree blockTree,
        IReceiptFinder receiptFinder,
        ISilCapabilitiesProvider capabilitiesProvider,
        IStateReader stateReader,
        ITxPool txPool,
        ITxSender txSender,
        IWallet wallet,
        ILogManager logManager,
        ISpecProvider specProvider,
        IGasPriceOracle gasPriceOracle,
        ISilSyncingInfo silSyncingInfo,
        IFeeHistoryOracle feeHistoryOracle,
        IProtocolsManager protocolsManager,
        IForkInfo forkInfo,
        IBlocksConfig blocksConfig,
        ISilaEcdsa ecdsa,
        IOptimismSpecHelper opSpecHelper,
        IOptimismConfig config,
        IJsonSerializer jsonSerializer,
        ITimestamper timestamper,
        ILogIndexConfig logIndexConfig,
        IReceiptConfig receiptConfig,
        IBlockForRpcFactory blockForRpcFactory
    )
    {
        _blockForRpcFactory = blockForRpcFactory;
        _secondsPerSlot = blocksConfig.SecondsPerSlot;
        _logManager = logManager;
        _stateReader = stateReader;
        _blockchainBridgeFactory = blockchainBridgeFactory;
        _txPool = txPool;
        _txSender = txSender;
        _wallet = wallet;
        _rpcConfig = rpcConfig;
        _specProvider = specProvider;
        _gasPriceOracle = gasPriceOracle;
        _ethSyncingInfo = silSyncingInfo;
        _feeHistoryOracle = feeHistoryOracle;
        _ecdsa = ecdsa;
        _blockFinder = blockFinder;
        _blockTree = blockTree;
        _receiptFinder = receiptFinder;
        _capabilitiesProvider = capabilitiesProvider;
        _opSpecHelper = opSpecHelper;
        _protocolsManager = protocolsManager;
        _forkInfo = forkInfo;
        _logIndexConfig = logIndexConfig;
        _receiptConfig = receiptConfig;
        ILogger logger = logManager.GetClassLogger<OptimismEthModuleFactory>();
        if (config.SequencerUrl is null && logger.IsWarn)
        {
            logger.Warn("SequencerUrl is not set. Nethermind will behave as a Sequencer");
        }

        BasicJsonRpcClient? sequencerJsonRpcClient = config.SequencerUrl is not null
            ? new(new Uri(config.SequencerUrl), jsonSerializer, logManager)
            : null;
        _sequencerRpcClient = sequencerJsonRpcClient;

        ITxSigner txSigner = new WalletTxSigner(wallet, specProvider.ChainId);
        TxSealer sealer = new(txSigner, timestamper);
        _sealer = sealer;
        _headBlockSignal = new HeadBlockSignal(blockTree);
    }

    public override IOptimismEthRpcModule Create() => new OptimismEthRpcModule(
            _rpcConfig,
            _blockchainBridgeFactory.CreateBlockchainBridge(),
            _blockFinder,
            _blockTree,
            _receiptFinder,
            _stateReader,
            _txPool,
            _txSender,
            _wallet,
            _logManager,
            _specProvider,
            _gasPriceOracle,
            _ethSyncingInfo,
            _feeHistoryOracle,
            _protocolsManager,
            _forkInfo,
            _secondsPerSlot,
            _sequencerRpcClient,
            _ecdsa,
            _sealer,
            _logIndexConfig,
            _receiptConfig,
            _opSpecHelper,
            _headBlockSignal,
            _capabilitiesProvider,
            _blockForRpcFactory
        );
}
