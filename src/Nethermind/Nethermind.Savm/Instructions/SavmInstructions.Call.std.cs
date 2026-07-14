// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Runtime.CompilerServices;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Savm.GasPolicy;
using Nethermind.Savm.Precompiles;
using Nethermind.Savm.State;
using Nethermind.Int256;

namespace Nethermind.Savm;

public static partial class SavmInstructions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static partial bool TryInlineStaticPrecompileCall<TGasPolicy, TTracingInst>(
        VirtualMachine<TGasPolicy> vm,
        ref SavmStack stack,
        ref TGasPolicy gas,
        in UInt256 dataOffset,
        UInt256 dataLength,
        in UInt256 outputOffset,
        UInt256 outputLength,
        IPrecompile precompile,
        Address target,
        Address codeSource,
        ulong gasLimitUl,
        out SavmExceptionType result)
        where TGasPolicy : struct, IGasPolicy<TGasPolicy>
        where TTracingInst : struct, IFlag
    {
        if (TTracingInst.IsActive || vm.TxTracer.IsTracingActions || !vm.CanExecutePrecompileCallDirectly(precompile, codeSource))
        {
            result = default;
            return false;
        }

        if (!vm.VmState.Memory.TryLoad(in dataOffset, dataLength, out ReadOnlyMemory<byte> callData))
        {
            result = SavmExceptionType.OutOfGas;
            return true;
        }

        TGasPolicy childGas = TGasPolicy.CreateChildFrameGas(ref gas, gasLimitUl);
        IReleaseSpec spec = vm.Spec;

        if (!TGasPolicy.ConsumePrecompileGas(ref childGas, precompile, callData, spec))
        {
            TGasPolicy.RestoreChildStateGasOnHalt(ref gas, in childGas);
            vm.ReturnDataBuffer = Array.Empty<byte>();
            vm.ReturnData = null;
            result = stack.PushZero<TTracingInst>();
            return true;
        }

        if (!(vm.TryRunPrecompileDirectly(precompile, callData, spec, out Result<byte[]> output) && output))
        {
            TGasPolicy.SetOutOfGas(ref childGas);
            TGasPolicy.RestoreChildStateGasOnHalt(ref gas, in childGas);
            vm.ReturnDataBuffer = Array.Empty<byte>();
            vm.ReturnData = null;
            result = stack.PushZero<TTracingInst>();
            return true;
        }

        vm.WorldState.AddToBalanceAndCreateIfNotExists(target, UInt256.Zero, spec);

        TGasPolicy.Refund(ref gas, in childGas);

        ReadOnlyMemory<byte> outputData = output.Data;
        vm.ReturnDataBuffer = outputData;

        int copyLength = outputData.Length;
        if (outputLength < (UInt256)copyLength)
            copyLength = (int)outputLength.ToLong();

        if (copyLength > 0)
        {
            ZeroPaddedSpan callOutput = outputData.Span.SliceWithZeroPadding(0, copyLength);
            if (!vm.VmState.Memory.TrySave(in outputOffset, in callOutput))
            {
                result = SavmExceptionType.OutOfGas;
                return true;
            }
        }

        vm.ReturnData = null;
        result = stack.PushBytes<TTracingInst>(StatusCode.SuccessBytes.Span);
        return true;
    }
}
