// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace Nethermind.Savm;

using Int256;

public static partial class SavmInstructions
{
    /// <summary>
    /// Fused <c>PUSH const; binary-op</c>: runs against the pre-decoded constant on the stack top —
    /// no push/pop, one dispatch. Preserves per-op failure order: push overflow before op underflow.
    /// </summary>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static SavmExceptionType FusedConstBinaryCore<TOpMath>(ref SavmStack stack, UInt256 a)
        where TOpMath : struct, IOpMath2Param
    {
        if (stack.Head == SavmStack.MaxStackSize - 1)
            return SavmExceptionType.StackOverflow;

        ref byte topRef = ref stack.PeekBytesByRef();
        if (IsNullRef(ref topRef)) return SavmExceptionType.StackUnderflow;

        SavmStack.ReadUInt256FromSlot(ref topRef, out UInt256 b);
        TOpMath.Operation(in a, in b, out UInt256 result);
        SavmStack.WriteUInt256ToSlot(ref topRef, in result);
        return SavmExceptionType.None;
    }

    /// <summary>Fused <c>PUSH shift-amount; SHL/SHR</c>, mirroring <see cref="ShiftCore{TOpShift, TTracingInst}"/>.</summary>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static SavmExceptionType FusedConstShiftCore<TOpShift>(ref SavmStack stack, UInt256 a)
        where TOpShift : struct, IOpShift
    {
        if (stack.Head == SavmStack.MaxStackSize - 1)
            return SavmExceptionType.StackOverflow;

        ref byte topRef = ref stack.PeekBytesByRef();
        if (IsNullRef(ref topRef)) return SavmExceptionType.StackUnderflow;

        // Mirrors ShiftCore: amounts of 256 or more shift everything out.
        if (!a.IsUint64 || a.u0 >= 256)
        {
            SavmStack.WriteUInt256ToSlot(ref topRef, in UInt256.Zero);
            return SavmExceptionType.None;
        }

        SavmStack.ReadUInt256FromSlot(ref topRef, out UInt256 b);
        TOpShift.Operation(in a, in b, out UInt256 result);
        SavmStack.WriteUInt256ToSlot(ref topRef, in result);
        return SavmExceptionType.None;
    }

    /// <summary>
    /// Fused <c>PUSH const; bitwise-op</c> over the stack-representation pool: one vector load per
    /// operand, no limb conversion.
    /// </summary>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static SavmExceptionType FusedConstBitwiseCore<TOpBitwise>(ref SavmStack stack, ref byte constantSlot)
        where TOpBitwise : struct, IOpBitwise
    {
        if (stack.Head == SavmStack.MaxStackSize - 1)
            return SavmExceptionType.StackOverflow;

        ref byte topRef = ref stack.PeekBytesByRef();
        if (IsNullRef(ref topRef)) return SavmExceptionType.StackUnderflow;

        SavmWord a = ReadUnaligned<SavmWord>(ref constantSlot);
        SavmWord b = ReadUnaligned<SavmWord>(ref topRef);
        WriteUnaligned(ref topRef, TOpBitwise.Operation(in a, in b));
        return SavmExceptionType.None;
    }
}
