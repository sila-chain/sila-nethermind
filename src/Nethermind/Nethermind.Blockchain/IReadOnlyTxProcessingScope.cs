// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Savm.State;
using Nethermind.Savm.TransactionProcessing;

namespace Nethermind.Blockchain;

public interface IReadOnlyTxProcessingScope : IDisposable
{
    ITransactionProcessor TransactionProcessor { get; }
    IWorldState WorldState { get; }
    void Reset() => Dispose();
}
