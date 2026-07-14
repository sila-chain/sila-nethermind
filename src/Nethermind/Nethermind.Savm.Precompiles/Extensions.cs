// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Specs;
using System.Collections.Generic;

namespace Nethermind.Savm.Precompiles;

public static class Extensions
{
    public static OrderedDictionary<string, Address> ListPrecompiles(this IReleaseSpec spec)
    {
        OrderedDictionary<string, Address> precompiles = [];

        AddPrecompile<ECRecoverPrecompile>();
        AddPrecompile<Sha256Precompile>();
        AddPrecompile<Ripemd160Precompile>();
        AddPrecompile<IdentityPrecompile>();

        if (spec.ModExpEnabled)
        {
            AddPrecompile<ModExpPrecompile>();
        }

        if (spec.BN254Enabled)
        {
            AddPrecompile<BN254AddPrecompile>();
            AddPrecompile<BN254MulPrecompile>();
            AddPrecompile<BN254PairingCheckPrecompile>();
        }

        if (spec.BlakeEnabled)
        {
            AddPrecompile<Blake2FPrecompile>();
        }

        if (spec.IsSip4844Enabled)
        {
            AddPrecompile<KzgPointEvaluationPrecompile>();
        }

        if (spec.Bls12381Enabled)
        {
            AddPrecompile<Bls12381G1AddPrecompile>();
            AddPrecompile<Bls12381G1MsmPrecompile>();
            AddPrecompile<Bls12381G2AddPrecompile>();
            AddPrecompile<Bls12381G2MsmPrecompile>();
            AddPrecompile<Bls12381PairingCheckPrecompile>();
            AddPrecompile<Bls12381FpToG1Precompile>();
            AddPrecompile<Bls12381Fp2ToG2Precompile>();
        }

        if (spec.IsSip7951Enabled)
        {
            AddPrecompile<SecP256r1Precompile>();
        }

        return precompiles;

        void AddPrecompile<T>() where T : IPrecompile<T> => precompiles[T.Name] = T.Address;
    }

    public static OrderedDictionary<string, Address> ListSystemContracts(this IReleaseSpec spec)
    {
        OrderedDictionary<string, Address> systemContracts = [];

        if (spec.IsBeaconBlockRootAvailable) systemContracts[Sip4788Constants.ContractAddressKey] = Sip4788Constants.BeaconRootsAddress;
        if (spec.ConsolidationRequestsEnabled) systemContracts[Sip7251Constants.ContractAddressKey] = Sip7251Constants.ConsolidationRequestPredeployAddress;
        if (spec.DepositsEnabled) systemContracts[Sip6110Constants.ContractAddressKey] = spec.DepositContractAddress!;
        if (spec.IsSip2935Enabled) systemContracts[Sip2935Constants.ContractAddressKey] = Sip2935Constants.BlockHashHistoryAddress;
        if (spec.WithdrawalRequestsEnabled) systemContracts[Sip7002Constants.ContractAddressKey] = Sip7002Constants.WithdrawalRequestPredeployAddress;

        return systemContracts;
    }
}
