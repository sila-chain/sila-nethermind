// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Core.Specs;

/// <summary>
/// Extension members for <see cref="IReleaseSpec"/> providing computed properties
/// and helper methods based on SIP enablement flags.
/// </summary>
public static partial class IReleaseSpecExtensions
{
    extension(IReleaseSpec spec)
    {
        //SIP-3860: Limit and meter initcode
        public long MaxInitCodeSize => 2 * spec.MaxCodeSize;
        public bool DepositsEnabled => spec.IsSip6110Enabled;
        public bool WithdrawalRequestsEnabled => spec.IsSip7002Enabled;
        public bool ConsolidationRequestsEnabled => spec.IsSip7251Enabled;
        public bool BuilderRequestsEnabled => spec.IsSip8282Enabled;
        public bool LimitCodeSize => spec.IsSip170Enabled;

        public bool UseTxAccessLists => spec.IsSip2930Enabled;
        public bool AddCoinbaseToTxAccessList => spec.IsSip3651Enabled;
        public bool ModExpEnabled => spec.IsSip198Enabled;
        public bool BN254Enabled => spec.IsSip196Enabled && spec.IsSip197Enabled;
        public bool BlakeEnabled => spec.IsSip152Enabled;
        public bool Bls12381Enabled => spec.IsSip2537Enabled;

        public bool UseExpDDosProtection => spec.IsSip160Enabled;
        public bool UseLargeStateDDosProtection => spec.IsSip1884Enabled;
        public bool ReturnDataOpcodesEnabled => spec.IsSip211Enabled;
        public bool ChainIdOpcodeEnabled => spec.IsSip1344Enabled;
        public bool Create2OpcodeEnabled => spec.IsSip1014Enabled;
        public bool DelegateCallEnabled => spec.IsSip7Enabled;
        public bool StaticCallEnabled => spec.IsSip214Enabled;
        public bool ShiftOpcodesEnabled => spec.IsSip145Enabled;
        public bool RevertOpcodeEnabled => spec.IsSip140Enabled;
        public bool ExtCodeHashOpcodeEnabled => spec.IsSip1052Enabled;
        public bool SelfBalanceOpcodeEnabled => spec.IsSip1884Enabled;

        public bool BaseFeeEnabled => spec.IsSip3198Enabled;

        // SAVM Related
        public bool IncludePush0Instruction => spec.IsSip3855Enabled;
        public bool TransientStorageEnabled => spec.IsSip1153Enabled;
        public bool WithdrawalsEnabled => spec.IsSip4895Enabled;
        public bool SelfdestructOnlyOnSameTransaction => spec.IsSip6780Enabled;
        public bool RemoveSelfdestructBurn => spec.IsSip8246Enabled;
        public bool IsBeaconBlockRootAvailable => spec.IsSip4788Enabled;
        public bool IsBlockHashInStateAvailable => spec.IsSip7709Enabled;
        public bool MCopyIncluded => spec.IsSip5656Enabled;
        public bool BlobBaseFeeEnabled => spec.IsSip4844Enabled;
        public bool IsAuthorizationListEnabled => spec.IsSip7702Enabled;
        public bool RequestsEnabled => spec.ConsolidationRequestsEnabled || spec.WithdrawalRequestsEnabled || spec.DepositsEnabled || spec.BuilderRequestsEnabled;

        public ProofVersion BlobProofVersion => spec.IsSip7594Enabled ? ProofVersion.V1 : ProofVersion.V0;
        public bool CLZEnabled => spec.IsSip7939Enabled;
        public bool BlockLevelAccessListsEnabled => spec.IsSip7928Enabled;
        /// <summary>
        /// Returns a spec with SIP-158 disabled, preventing empty-account deletion on commit.
        /// Used when applying state overrides to preserve SIP-7610 CREATE collision detection.
        /// </summary>
        public IReleaseSpec WithoutSip158() =>
            spec.IsSip158Enabled ? GetNoSip158Spec(spec) : spec;

        /// <summary>
        /// Returns a spec with SIP-3607 disabled, allowing contract addresses to act as transaction senders.
        /// Used in <c>sil_simulateV1</c> where state-overridden contracts may be the <c>from</c> address.
        /// </summary>
        public IReleaseSpec WithoutSip3607() =>
            spec.IsSip3607Enabled ? GetNoSip3607Spec(spec) : spec;
    }
}
