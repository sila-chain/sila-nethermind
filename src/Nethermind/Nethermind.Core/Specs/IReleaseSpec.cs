// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Nethermind.Int256;

namespace Nethermind.Core.Specs
{
    /// <summary>
    /// https://github.com/sila-chain/SIPs
    /// </summary>
    public interface IReleaseSpec : ISip1559Spec, IReceiptSpec
    {
        public string Name { get; }
        long MaximumExtraDataSize { get; }
        long MaxCodeSize { get; }
        ulong MinGasLimit { get; }
        ulong MinHistoryRetentionEpochs { get; }
        ulong MinBalRetentionEpochs { get; }
        ulong GasLimitBoundDivisor { get; }
        UInt256 BlockReward { get; }
        ulong DifficultyBombDelay { get; }
        ulong DifficultyBoundDivisor { get; }
        ulong? FixedDifficulty { get; }
        int MaximumUncleCount { get; }

        /// <summary>
        /// ---
        /// In chainspec - Silash.Duration
        /// </summary>
        bool IsTimeAdjustmentPostOlympic { get; }

        /// <summary>
        /// Homestead contract creation via transaction cost set to 21000 + 32000 (previously 21000)
        /// Failing init does not create an empty code contract
        /// Difficulty adjustment changed
        /// Transaction signature uniqueness (s-value has to be less or equal than than secp256k1n/2)
        /// </summary>
        bool IsSip2Enabled { get; }

        /// <summary>
        /// Homestead DELEGATECALL instruction added
        /// </summary>
        bool IsSip7Enabled { get; }

        /// <summary>
        /// Byzantium Change difficulty adjustment to target mean block time including uncles
        /// </summary>
        bool IsSip100Enabled { get; }

        /// <summary>
        /// Byzantium REVERT instruction in the Sila Virtual Machine
        /// ---
        /// in chainspec Silash.Sip100bTransition
        /// </summary>
        bool IsSip140Enabled { get; }

        /// <summary>
        /// Tangerine Whistle Gas cost of IO operations increased
        /// </summary>
        bool IsSip150Enabled { get; }

        /// <summary>
        /// Spurious Dragon Chain ID in signatures (replay attack protection)
        /// </summary>
        bool IsSip155Enabled { get; }

        /// <summary>
        /// Spurious Dragon State clearing
        /// </summary>
        bool IsSip158Enabled { get; }

        /// <summary>
        /// Spurious Dragon EXP cost increase
        /// </summary>
        bool IsSip160Enabled { get; }

        /// <summary>
        /// Spurious Dragon Code size limit
        /// ---
        /// in chainspec MaxCodeSizeTransition
        /// </summary>
        bool IsSip170Enabled { get; }

        /// <summary>
        /// Byzantium Precompiled contracts for addition and scalar multiplication on the elliptic curve alt_bn128
        /// ---
        /// in chainspec in builtin accounts
        /// </summary>
        bool IsSip196Enabled { get; }

        /// <summary>
        /// Byzantium Precompiled contracts for optimal ate pairing check on the elliptic curve alt_bn128
        /// ---
        /// in chainspec in builtin accounts
        /// </summary>
        bool IsSip197Enabled { get; }

        /// <summary>
        /// Byzantium Precompiled contract for bigint modular exponentiation
        /// ---
        /// in chainspec in builtin accounts
        /// </summary>
        bool IsSip198Enabled { get; }

        /// <summary>
        /// Byzantium New opcodes: RETURNDATASIZE and RETURNDATACOPY
        /// </summary>
        bool IsSip211Enabled { get; }

        /// <summary>
        /// Byzantium New opcode STATICCALL
        /// </summary>
        bool IsSip214Enabled { get; }

        /// <summary>
        /// Byzantium Difficulty Bomb Delay and Block Reward Reduction
        /// ---
        /// in chainspec as DifficultyBombDelays
        /// </summary>
        bool IsSip649Enabled { get; }

        /// <summary>
        /// Constantinople SHL, SHR, SAR instructions
        /// </summary>
        bool IsSip145Enabled { get; }

        /// <summary>
        /// Constantinople Skinny CREATE2
        /// </summary>
        bool IsSip1014Enabled { get; }

        /// <summary>
        /// Constantinople EXTCODEHASH instructions
        /// </summary>
        bool IsSip1052Enabled { get; }

        /// <summary>
        /// Constantinople Net gas metering for SSTORE operations
        /// </summary>
        bool IsSip1283Enabled { get; }

        /// <summary>
        /// Constantinople Difficulty Bomb Delay and Block Reward Adjustment
        /// ---
        /// in chainspec as DifficultyBombDelays and BlockReward
        /// </summary>
        bool IsSip1234Enabled { get; }

        /// <summary>
        /// Istanbul ChainID opcode
        /// </summary>
        bool IsSip1344Enabled { get; }

        /// <summary>
        /// Istanbul transaction data gas cost reduction
        /// </summary>
        bool IsSip2028Enabled { get; }

        /// <summary>
        /// Istanbul Blake2F precompile
        /// </summary>
        bool IsSip152Enabled { get; }

        /// <summary>
        /// Istanbul alt_bn128 gas cost reduction
        /// </summary>
        bool IsSip1108Enabled { get; }

        /// <summary>
        /// Istanbul state opcodes gas cost increase
        /// </summary>
        bool IsSip1884Enabled { get; }

        /// <summary>
        /// Istanbul net-metered SSTORE
        /// </summary>
        bool IsSip2200Enabled { get; }

        /// <summary>
        /// Berlin BLS crypto precompiles
        /// </summary>
        bool IsSip2537Enabled { get; }

        /// <summary>
        /// Berlin MODEXP precompiles
        /// </summary>
        bool IsSip2565Enabled { get; }

        /// <summary>
        /// Berlin gas cost increases for state reading opcodes
        /// </summary>
        bool IsSip2929Enabled { get; }

        /// <summary>
        /// Berlin access lists
        /// </summary>
        bool IsSip2930Enabled { get; }

        /// <summary>
        /// Account for which SIP-158 state clearing should be ignored.
        /// </summary>
        /// <remarks>This is needed for SystemUser account compatibility with Parity on AuRa chains.</remarks>
        Address? Sip158IgnoredAccount => null;

        /// <summary>
        /// BaseFee opcode
        /// </summary>
        bool IsSip3198Enabled { get; }

        /// <summary>
        /// Reduction in refunds
        /// </summary>
        bool IsSip3529Enabled { get; }

        /// <summary>
        /// Reject new contracts starting with the 0xEF byte
        /// </summary>
        bool IsSip3541Enabled { get; }

        /// <summary>
        /// Reject transactions where senders have non-empty code hash
        /// </summary>
        bool IsSip3607Enabled { get; }

        /// <summary>
        /// Warm COINBASE
        /// </summary>
        bool IsSip3651Enabled { get; }

        /// <summary>
        /// Transient storage
        /// </summary>
        bool IsSip1153Enabled { get; }


        /// <summary>
        /// PUSH0 instruction
        /// </summary>
        bool IsSip3855Enabled { get; }

        /// <summary>
        /// MCOPY instruction
        /// </summary>
        bool IsSip5656Enabled { get; }

        /// <summary>
        /// SIP-3860: Limit and meter initcode
        /// </summary>
        bool IsSip3860Enabled { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the
        /// <see href="https://sips.sila.org/SIPS/sip-4895">SIP-4895</see>
        /// validator withdrawals are enabled.
        /// </summary>
        bool IsSip4895Enabled { get; }

        /// <summary>
        /// Blob transactions
        /// </summary>
        bool IsSip4844Enabled { get; }

        /// <summary>
        /// Parent Beacon Block precompile
        /// </summary>
        bool IsSip4788Enabled { get; }
        Address? Sip4788ContractAddress { get; }


        /// <summary>
        /// SIP-6110: Supply validator deposits on chain
        /// </summary>
        bool IsSip6110Enabled { get; }
        [MemberNotNullWhen(true, nameof(IsSip6110Enabled))]
        Address? DepositContractAddress { get; }

        /// <summary>
        /// Execution layer triggerable exits
        /// </summary>
        bool IsSip7002Enabled { get; }
        [MemberNotNullWhen(true, nameof(Sip7002ContractAddress))]
        Address? Sip7002ContractAddress { get; }


        /// <summary>
        /// SIP-7251: triggered consolidations
        /// </summary>
        bool IsSip7251Enabled { get; }
        [MemberNotNullWhen(true, nameof(IsSip7251Enabled))]
        Address? Sip7251ContractAddress { get; }


        /// <summary>
        /// Save historical block hashes in state
        /// </summary>
        bool IsSip2935Enabled { get; }

        /// <summary>
        /// Fetch blockHashes from the state for BLOCKHASH opCode
        /// </summary>
        bool IsSip7709Enabled { get; }
        [MemberNotNullWhen(true, nameof(Sip2935ContractAddress))]
        Address? Sip2935ContractAddress { get; }

        /// <summary>
        /// SIP-2935 ring buffer size for historical block hash storage.
        /// Defaults to 8,191 blocks for Sila sila-mainnet.
        /// </summary>
        public ulong Sip2935RingBufferSize { get; }

        /// <summary>
        /// SELFDESTRUCT only in same transaction
        /// </summary>
        bool IsSip6780Enabled { get; }

        /// <summary>
        /// SIP-8282: builder execution requests (builder deposit + builder exit predeploys).
        /// </summary>
        bool IsSip8282Enabled { get; }

        /// <summary>
        /// SIP-8038: State-access gas cost update
        /// </summary>
        bool IsSip8038Enabled { get; }

        /// <summary>
        /// SIP-8024: Backward-compatible SWAPN, DUPN, EXCHANGE
        /// </summary>
        bool IsSip8024Enabled { get; }

        /// <summary>
        /// Transactions that allows code delegation for EOA
        /// </summary>
        bool IsSip7702Enabled { get; }

        /// <summary>
        /// Set upper bounds for MODEXP
        /// </summary>
        bool IsSip7823Enabled { get; }

        /// <summary>
        /// Blob base fee bounded by execution cost
        /// </summary>
        bool IsSip7918Enabled { get; }

        /// <summary>
        /// Blob base fee collection for Gnosis
        /// </summary>
        bool IsSip4844FeeCollectorEnabled { get; }

        /// <summary>
        /// SecP256r1 precompile
        /// </summary>
        bool IsRip7212Enabled { get; }
        bool IsSip7951Enabled { get; }

        /// <summary>
        ///  Increase call data cost
        /// </summary>
        bool IsSip7623Enabled { get; }

        /// <summary>
        ///  Transaction gas limit cap
        /// </summary>
        bool IsSip7825Enabled { get; }

        /// <summary>
        ///  Increase ModExp Gas Cost
        /// </summary>
        bool IsSip7883Enabled { get; }

        /// <summary>
        ///  RLP Execution Block Size Limit
        /// </summary>
        bool IsSip7934Enabled { get; }
        int Sip7934MaxRlpBlockSize { get; }

        /// <summary>
        ///  Increase Calldata Floor Cost
        /// </summary>
        bool IsSip7976Enabled { get; }

        /// <summary>
        /// Access List Token Floor Pricing
        /// </summary>
        bool IsSip7981Enabled { get; }

        /// <summary>
        /// Should transactions be validated against chainId.
        /// </summary>
        /// <remarks>Backward compatibility for early SilaKovan blocks.</remarks>
        public bool ValidateChainId { get; }

        /// <summary>
        /// SIP-7780: Add blob schedule to EL config files
        /// </summary>
        public ulong TargetBlobCount { get; }
        public ulong MaxBlobCount { get; }
        public ulong MaxBlobsPerTx { get; }
        public ulong BlobBaseFeeUpdateFraction { get; }

        public ulong WithdrawalTimestamp { get; }

        public ulong Sip4844TransitionTimestamp { get; }

        public bool IsSip7594Enabled { get; }


        /// <summary>
        /// Gets a cached set of all precompiled contract addresses for this release specification.
        /// Chain-specific implementations can override this to include their own precompiled contracts.
        /// </summary>
        FrozenSet<AddressAsKey> Precompiles { get; }

        /// <summary>
        /// SIP-7939 - CLZ - Count leading zeros instruction
        /// </summary>
        public bool IsSip7939Enabled { get; }

        /// <summary>
        /// SIP-7928: Block-Level Access Lists
        /// </summary>
        public bool IsSip7928Enabled { get; }
        bool BlockLevelAccessListsEnabled => IsSip7928Enabled;

        /// <summary>
        /// SIP-8037: Cost Per State Byte / State Size Limit.
        /// Two-dimensional gas metering for state growth control.
        /// </summary>
        public bool IsSip8037Enabled { get; }

        /// <summary>
        /// SIP-7708: SIL transfers and burns emit a log
        /// </summary>
        public bool IsSip7708Enabled { get; }

        /// <summary>
        /// SIP-7843: SLOTNUM opcode
        /// </summary>
        public bool IsSip7843Enabled { get; }

        /// <summary>
        /// SIP-7954: Increase Maximum Contract Size
        /// </summary>
        public bool IsSip7954Enabled { get; }

        /// <summary>
        /// SIP-8246: SELFDESTRUCT no longer burns SIL
        /// </summary>
        public bool IsSip8246Enabled { get; }

        /// <summary>
        /// SIP-2780: Reduce intrinsic transaction gas (TX_BASE_COST) and reprice value-transfer
        /// and cold-account costs against actual state work.
        /// </summary>
        /// <remarks>Must be co-activated with SIP-7708: the value-transfer cost prices the transfer log.</remarks>
        public bool IsSip2780Enabled { get; }

        /// <summary>
        /// Precomputed gas cost and refund constants derived from this spec.
        /// Values are cached per spec instance (singletons per fork) to avoid
        /// repeated interface dispatch on the SAVM opcode hot path.
        /// </summary>
        SpecGasCosts GasCosts { get; }
    }
}
