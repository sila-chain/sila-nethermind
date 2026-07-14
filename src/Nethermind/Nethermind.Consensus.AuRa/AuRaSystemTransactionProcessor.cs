// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Savm;
using Nethermind.Savm.GasPolicy;
using Nethermind.Savm.State;
using Nethermind.Savm.TransactionProcessing;
using Nethermind.Int256;
using Nethermind.Logging;

namespace Nethermind.Consensus.AuRa;

/// <summary>
/// AuRa-flavoured system transaction processor. Surfaces system-user reads to the BAL,
/// materialises SYSTEM_ADDRESS on non-genesis system calls, and keeps SIP-158 disabled for
/// system-tx state commits at genesis.
/// </summary>
public sealed class AuRaSystemTransactionProcessor<TGasPolicy>(
    ITransactionProcessor.IBlobBaseFeeCalculator blobBaseFeeCalculator,
    ISpecProvider? specProvider,
    IWorldState? worldState,
    IVirtualMachine<TGasPolicy>? virtualMachine,
    ICodeInfoRepository? codeInfoRepository,
    ILogManager? logManager)
    : SystemTransactionProcessor<TGasPolicy>(blobBaseFeeCalculator, specProvider, worldState, virtualMachine, codeInfoRepository, logManager)
    where TGasPolicy : struct, IGasPolicy<TGasPolicy>
{
    protected override bool ShouldSuppressSystemAccountReads(Transaction tx) => false;

    protected override void OnBeforeSystemTransaction()
    {
        // Materialise SYSTEM_ADDRESS only off-genesis. Note this genesis check is deliberately
        // independent of SkipSystemSpecWrap below, which never skips (SIP-158 stays off even at genesis).
        if (!VirtualMachine.BlockExecutionContext.IsGenesis)
        {
            WorldState.CreateAccountIfNotExists(Address.SystemUser, UInt256.Zero, 0);
        }
    }

    protected override bool SkipSystemSpecWrap(BlockHeader header) => false;
}
