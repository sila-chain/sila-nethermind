// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Int256;
using Nethermind.Specs.ChainSpecStyle.Json;

namespace Nethermind.Specs.ChainSpecStyle;

public class ChainParameters
{
    public long? MaxCodeSize { get; set; }
    public ulong? MaxCodeSizeTransition { get; set; }
    public ulong? MaxCodeSizeTransitionTimestamp { get; set; }
    public ulong GasLimitBoundDivisor { get; set; }
    public Address Registrar { get; set; }
    public long MaximumExtraDataSize { get; set; }
    public ulong MinGasLimit { get; set; }
    public ulong MinHistoryRetentionEpochs { get; set; }
    public ulong MinBalRetentionEpochs { get; set; }
    public Hash256 ForkCanonHash { get; set; }
    public ulong? ForkBlock { get; set; }
    public ulong? Sip7Transition { get; set; }
    public ulong? Sip150Transition { get; set; }
    public ulong? Sip152Transition { get; set; }
    public ulong? Sip160Transition { get; set; }
    public ulong? Sip161abcTransition { get; set; }
    public ulong? Sip161dTransition { get; set; }
    public ulong? Sip155Transition { get; set; }
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
    public ulong? Sip2537TransitionTimestamp { get; set; }
    public ulong? Sip2565Transition { get; set; }
    public ulong? Sip2929Transition { get; set; }
    public ulong? Sip2930Transition { get; set; }
    public ulong? Sip3198Transition { get; set; }
    public ulong? Sip3529Transition { get; set; }

    public ulong? Sip3541Transition { get; set; }
    public ulong? Sip3607Transition { get; set; }

    public UInt256? Sip1559BaseFeeInitialValue { get; set; }

    public UInt256? Sip1559BaseFeeMaxChangeDenominator { get; set; }

    public ulong? Sip1559ElasticityMultiplier { get; set; }

    /// <summary>
    ///  Transaction permission managing contract address.
    /// </summary>
    public Address TransactionPermissionContract { get; set; }
    /// <summary>
    /// Block at which the transaction permission contract should start being used.
    /// </summary>
    public ulong? TransactionPermissionContractTransition { get; set; }

    /// <summary>
    /// Optional, will be included for block 0 by default - Block before which any chain_id in the signature of a replay-protected transaction is accepted.
    /// After this transition block, the transactions' chain_id must match with the spec chain_id to be considered valid.
    /// </summary>
    /// <remarks>Backward compatibility for early SilaKovan blocks.</remarks>
    public ulong? ValidateChainIdTransition { get; set; }

    /// <summary>
    /// Optional, will be included for block 0 by default - Transition block before which the state root in transaction's receipt can be stripped.
    /// </summary>
    /// <returns></returns>
    public ulong? ValidateReceiptsTransition { get; set; }

    /// <summary>
    /// Block from which burnt SIP-1559 fees will go to <see cref="Sip1559FeeCollector"/>
    /// </summary>
    public ulong? Sip1559FeeCollectorTransition { get; set; }

    /// <summary>
    /// Optional, address where burnt SIP-1559 fees will go
    /// </summary>
    public Address FeeCollector { get; set; }

    /// <summary>
    /// Block from which SIP1559 base fee cannot drop below <see cref="Sip1559BaseFeeMinValue"/>
    /// </summary>
    public ulong? Sip1559BaseFeeMinValueTransition { get; set; }

    /// <summary>
    /// Optional, minimal value of SIP1559 base fee
    /// </summary>
    public UInt256? Sip1559BaseFeeMinValue { get; set; }

    public ulong? MergeForkIdTransition { get; set; }

    public ulong? TerminalPoWBlockNumber { get; set; }

    public UInt256? TerminalTotalDifficulty { get; set; }
    public ulong? BeaconChainGenesisTimestamp { get; set; }
    public ulong? Sip3651Transition { get; set; }
    public ulong? Sip3651TransitionTimestamp { get; set; }
    public ulong? Sip3855Transition { get; set; }
    public ulong? Sip3855TransitionTimestamp { get; set; }
    public ulong? Sip3860Transition { get; set; }
    public ulong? Sip3860TransitionTimestamp { get; set; }
    public ulong? Sip4895TransitionTimestamp { get; set; }
    public ulong? Sip4844TransitionTimestamp { get; set; }
    public ulong? Sip4844Transition { get; set; }
    public ulong? Sip1153Transition { get; set; }
    public ulong? Sip1153TransitionTimestamp { get; set; }
    public ulong? Sip5656Transition { get; set; }
    public ulong? Sip5656TransitionTimestamp { get; set; }
    public ulong? Sip6780Transition { get; set; }
    public ulong? Sip6780TransitionTimestamp { get; set; }
    public ulong? Sip4788TransitionTimestamp { get; set; }
    public Address Sip4788ContractAddress { get; set; }
    public ulong? Sip6110TransitionTimestamp { get; set; }
    public Address DepositContractAddress { get; set; }
    public ulong? Sip7002TransitionTimestamp { get; set; }
    public Address Sip7002ContractAddress { get; set; }
    public ulong? Sip7251TransitionTimestamp { get; set; }
    public Address Sip7251ContractAddress { get; set; }
    public ulong? Sip2935Transition { get; set; }
    public ulong? Sip2935TransitionTimestamp { get; set; }
    public Address Sip2935ContractAddress { get; set; }
    public ulong Sip2935RingBufferSize { get; set; } = Sip2935Constants.RingBufferSize;
    public ulong? Sip7951TransitionTimestamp { get; set; }
    public ulong? Rip7212TransitionTimestamp { get; set; }
    public ulong? Sip7702Transition { get; set; }
    public ulong? Sip7702TransitionTimestamp { get; set; }

    public ulong? Sip7594TransitionTimestamp { get; set; }
    public ulong? Sip7623Transition { get; set; }
    public ulong? Sip7623TransitionTimestamp { get; set; }
    public ulong? Sip7778TransitionTimestamp { get; set; }
    public ulong? Sip7823TransitionTimestamp { get; set; }
    public ulong? Sip7825TransitionTimestamp { get; set; }
    public ulong? Sip7883TransitionTimestamp { get; set; }
    public ulong? Sip7918TransitionTimestamp { get; set; }
    public ulong? Sip7976TransitionTimestamp { get; set; }
    public ulong? Sip7981TransitionTimestamp { get; set; }

    public ulong? Sip7934TransitionTimestamp { get; set; }
    public int Sip7934MaxRlpBlockSize { get; set; }

    public SortedSet<BlobScheduleSettings>? BlobSchedule { get; set; } = [];

    #region SIP-4844 parameters
    /// <summary>
    /// Gets or sets the <c>BLOB_GASPRICE_UPDATE_FRACTION</c> parameter defined in
    /// <see href="https://sips.sila.org/SIPS/sip-4844#parameters">SIP-4844</see>.
    /// </summary>
    public ulong? Sip4844BlobGasPriceUpdateFraction { get; set; }

    /// <summary>
    /// Gets or sets the <c>MIN_BLOB_GASPRICE</c> parameter, in wei, defined in
    /// <see href="https://sips.sila.org/SIPS/sip-4844#parameters">SIP-4844</see>.
    /// </summary>
    public UInt256? Sip4844MinBlobGasPrice { get; set; }

    /// <summary>
    /// Enables blob gas fee collection for Gnosis chain
    /// </summary>
    public ulong? Sip4844FeeCollectorTransitionTimestamp { get; set; }

    public ulong? Sip7939TransitionTimestamp { get; set; }

    #endregion

    public ulong? Sip8037TransitionTimestamp { get; set; }
    public ulong? Sip7928TransitionTimestamp { get; set; }

    public ulong? Sip7708TransitionTimestamp { get; set; }
    public ulong? Sip8024TransitionTimestamp { get; set; }
    public ulong? Sip8246TransitionTimestamp { get; set; }
    public ulong? Sip8038TransitionTimestamp { get; set; }
    public ulong? Sip8282TransitionTimestamp { get; set; }
    public ulong? Sip7843TransitionTimestamp { get; set; }
    public ulong? Sip7954TransitionTimestamp { get; set; }
    public ulong? Sip2780TransitionTimestamp { get; set; }
}
