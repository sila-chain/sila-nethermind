// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Savm
{
    public static class ReleaseSpecExtensions
    {
        extension(IReleaseSpec spec)
        {
            public ulong MaxProductionBlobCount(int? blockProductionBlobLimit) =>
                blockProductionBlobLimit >= 0
                    ? Math.Min((ulong)blockProductionBlobLimit.Value, spec.MaxBlobCount)
                    : spec.MaxBlobCount;

            public ulong GetBaseDataCost(Transaction tx) =>
                tx.IsContractCreation && spec.IsSip3860Enabled
                    ? SavmCalculations.Div32Ceiling((UInt256)tx.Data.Length) * GasCostOf.InitCodeWord
                    : 0;
        }
    }
}
