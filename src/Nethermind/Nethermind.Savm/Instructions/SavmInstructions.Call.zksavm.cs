// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Savm.GasPolicy;
using Nethermind.Savm.Precompiles;
using Nethermind.Int256;

namespace Nethermind.Savm;

public static partial class SavmInstructions
{
    // The zkSAVM guest handles STATICCALL precompiles through its dedicated InlinePrecompileCall path in
    // CreateFullCallFrame, so the inline fast path always declines here.
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
        result = default;
        return false;
    }
}
