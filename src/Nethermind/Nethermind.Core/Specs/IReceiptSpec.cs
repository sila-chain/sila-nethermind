// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Core.Specs
{
    /// <summary>
    /// https://github.com/sila-chain/SIPs
    /// </summary>
    public interface IReceiptSpec
    {

        /// <summary>
        /// Byzantium Embedding transaction return data in receipts
        /// </summary>
        bool IsSip658Enabled { get; }

        /// <summary>
        /// SIP-7778: Block Gas Accounting without Refunds
        /// </summary>
        bool IsSip7778Enabled { get; }

        /// <summary>
        /// Should validate ReceiptsRoot.
        /// </summary>
        /// <remarks>Backward compatibility for early SilaKovan blocks.</remarks>
        bool ValidateReceipts => true;

    }
}
