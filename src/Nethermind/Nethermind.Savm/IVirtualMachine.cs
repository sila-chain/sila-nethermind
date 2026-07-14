// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Savm.GasPolicy;
using Nethermind.Savm.State;
using Nethermind.Savm.Tracing;

namespace Nethermind.Savm;

public interface IVirtualMachine<TGasPolicy>
    where TGasPolicy : struct, IGasPolicy<TGasPolicy>
{
    TransactionSubstate ExecuteTransaction<TTracingInst>(VmState<TGasPolicy> state, IWorldState worldState, ITxTracer txTracer)
        where TTracingInst : struct, IFlag;

    TransactionSubstate ExecuteTransaction(VmState<TGasPolicy> state, IWorldState worldState, ITxTracer txTracer)
        => ExecuteTransaction<OffFlag>(state, worldState, txTracer);

    ref readonly BlockExecutionContext BlockExecutionContext { get; }
    ref readonly TxExecutionContext TxExecutionContext { get; }
    void SetBlockExecutionContext(in BlockExecutionContext blockExecutionContext);
    void SetTxExecutionContext(in TxExecutionContext txExecutionContext);
    int OpCodeCount { get; }
    void FlushMetricsCounters();
}

/// <summary>
/// Non-generic IVirtualMachine for backward compatibility with SilaGasPolicy.
/// </summary>
public interface IVirtualMachine : IVirtualMachine<SilaGasPolicy>
{
}
