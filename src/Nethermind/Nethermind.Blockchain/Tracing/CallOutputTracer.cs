// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Savm.Tracing;
using Nethermind.Savm.TransactionProcessing;

namespace Nethermind.Blockchain.Tracing;

public class CallOutputTracer : TxTracer
{
    public override bool IsTracingReceipt => true;
    public byte[]? ReturnValue { get; set; }

    public ulong GasSpent { get; set; }
    public ulong OperationGas { get; set; }

    public string? Error { get; set; }

    public byte StatusCode { get; set; }

    public override void MarkAsSuccess(Address recipient, in GasConsumed gasSpent, byte[] output, LogEntry[] logs,
        Hash256? stateRoot = null)
    {
        GasSpent = gasSpent.SpentGas;
        OperationGas = gasSpent.OperationGas;
        ReturnValue = output;
        StatusCode = Savm.StatusCode.Success;
    }

    public override void MarkAsFailed(Address recipient, in GasConsumed gasSpent, byte[] output, string? error,
        Hash256? stateRoot = null)
    {
        GasSpent = gasSpent.SpentGas;
        OperationGas = gasSpent.OperationGas;
        Error = error;
        ReturnValue = output;
        StatusCode = Savm.StatusCode.Failure;
    }

    public void Reset()
    {
        GasSpent = 0;
        OperationGas = 0;
        ReturnValue = null;
        Error = null;
        StatusCode = 0;
    }
}
