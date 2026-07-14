// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Savm;
using Nethermind.Savm.State;
using Nethermind.Savm.Tracing;
using Nethermind.Savm.TransactionProcessing;

namespace Nethermind.Consensus.Processing;

/// <summary>
/// Reports the main thread's per-transaction progress to the prewarmer so it can skip warming already-started txs.
/// The <see cref="IPrewarmerState.IsPrewarmer"/> guard ensures only the main execution reports, not the prewarmer's own scope.
/// </summary>
public class PrewarmerTxAdapter(ITransactionProcessorAdapter baseAdapter, BlockCachePreWarmer preWarmer, IPrewarmerState prewarmerState) : ITransactionProcessorAdapter
{
    public TransactionResult Execute(Transaction transaction, ITxTracer txTracer)
    {
        ReportProgress();
        return baseAdapter.Execute(transaction, txTracer);
    }

    private void ReportProgress()
    {
        if (!prewarmerState.IsPrewarmer)
        {
            preWarmer.OnBeforeTxExecution();
        }
    }

    public void SetBlockExecutionContext(in BlockExecutionContext blockExecutionContext) => baseAdapter.SetBlockExecutionContext(in blockExecutionContext);
}
