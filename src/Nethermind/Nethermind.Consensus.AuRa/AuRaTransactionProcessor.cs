// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Specs;
using Nethermind.Savm;
using Nethermind.Savm.GasPolicy;
using Nethermind.Savm.State;
using Nethermind.Savm.TransactionProcessing;
using Nethermind.Logging;

namespace Nethermind.Consensus.AuRa;

/// <summary>
/// AuRa-flavoured <see cref="SilaTransactionProcessorBase"/> — overrides system-tx creation so
/// every TransactionProcessor instance built for the AuRa chain (DI singleton, BAL workers,
/// tests) routes system transactions through <see cref="AuRaSystemTransactionProcessor{TGasPolicy}"/>.
/// </summary>
public sealed class AuRaSilaTransactionProcessor(
    ITransactionProcessor.IBlobBaseFeeCalculator blobBaseFeeCalculator,
    ISpecProvider? specProvider,
    IWorldState? worldState,
    IVirtualMachine? virtualMachine,
    ICodeInfoRepository? codeInfoRepository,
    ILogManager? logManager,
    bool parallel = false)
    : SilaTransactionProcessorBase(blobBaseFeeCalculator, specProvider, worldState, virtualMachine, codeInfoRepository, logManager, parallel)
{
    protected override SystemTransactionProcessor<SilaGasPolicy> CreateSystemTransactionProcessor() =>
        new AuRaSystemTransactionProcessor<SilaGasPolicy>(
            _blobBaseFeeCalculator, SpecProvider, WorldState, VirtualMachine, _codeInfoRepository, _logManager);
}

/// <summary>Builds AuRa-aware worker tx processors for the BAL pool.</summary>
public sealed class AuRaTransactionProcessorFactory : ITransactionProcessorFactory
{
    public ITransactionProcessor Create(
        ITransactionProcessor.IBlobBaseFeeCalculator blobBaseFeeCalculator,
        ISpecProvider specProvider,
        IWorldState worldState,
        IVirtualMachine virtualMachine,
        ICodeInfoRepository codeInfoRepository,
        ILogManager logManager,
        bool parallel)
        => new AuRaSilaTransactionProcessor(
            blobBaseFeeCalculator, specProvider, worldState, virtualMachine,
            codeInfoRepository, logManager, parallel);
}
