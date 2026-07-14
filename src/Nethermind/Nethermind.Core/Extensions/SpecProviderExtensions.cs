// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Specs;

namespace Nethermind.Core.Extensions
{
    public static class SpecProviderExtensions
    {
        /// <summary>
        /// This method only retrieves the spec related to receipts.
        /// Reason for adding it is that sometimes we don't know the timestamp.
        /// </summary>
        /// <param name="specProvider"></param>
        /// <param name="blockNumber"></param>
        /// <returns>IReceiptSpec</returns>
        public static IReceiptSpec GetReceiptSpec(this ISpecProvider specProvider, ulong blockNumber) => specProvider.GetSpec(blockNumber, null);

        /// <summary>
        /// This method only retrieves the spec for 1559.
        /// Reason for adding it is that sometimes we don't know the timestamp.
        /// </summary>
        /// <param name="specProvider"></param>
        /// <param name="blockNumber"></param>
        /// <returns>ISip1559Spec</returns>
        public static ISip1559Spec GetSpecFor1559(this ISpecProvider specProvider, ulong blockNumber) => specProvider.GetSpec(blockNumber, null);

        public static ulong GetFinalMaxBlobGasPerBlock(this ISpecProvider specProvider) => Sip4844Constants.GasPerBlob * specProvider.GetFinalSpec().MaxBlobCount;
    }
}
