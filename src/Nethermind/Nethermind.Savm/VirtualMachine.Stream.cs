// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Nethermind.Core;
using Nethermind.Savm.CodeAnalysis;

namespace Nethermind.Savm;

/// <summary>Process-wide switches for the preprocessed-stream interpreter; non-generic so all instantiations share one flag.</summary>
internal static class StreamInterpreter
{
    // On by default; the gate restricts it to cancelable (sil_call/simulation) frames. Volatile so a test
    // flipping it in-process is visible to frame-executing threads.
    public static volatile bool Enabled = true;

    // The stream is a compute optimization with no payoff on storage-bound block processing, where it is
    // pure overhead (build cost + retained StreamOp[]). Production engages it only in cancelable call
    // contexts (sil_call/estimateGas/simulate). Differential tests set this to exercise the stream in any
    // context regardless of that heuristic.
    public static volatile bool ForceAllContexts;

    // Executions before a CodeInfo's stream is built; keeps the one-time build off cold code. Minimum 1.
    public static int BuildThreshold = 4;

    // Streams over this size aren't retained (fall back to the metered loop); 256 KiB covers any SIP-170 contract.
    public const int MaxStreamRetainedBytes = 256 * 1024;

    // Per-thread diagnostic counter of stream frames executed, read by differential tests to assert the
    // stream engaged. [ThreadStatic] so each thread bumps its own slot with a plain write: no atomic and
    // no cross-core cache-line bouncing on the hot RunStream entry. Tests run single-threaded, so they
    // observe their own thread's count. Not a true global total — that is not needed.
    [ThreadStatic] public static long FramesExecuted;
}

public unsafe partial class VirtualMachine<TGasPolicy>
{
    /// <summary>
    /// Executes a frame over the preprocessed <see cref="InstructionStream"/>: per-block static gas charged
    /// once at each block's first entry, in-block ops run gas-free cores. Non-tracing tip-fork frames only.
    /// A block whose precharge exceeds remaining gas, or one entered past its charging entry, falls to the
    /// metered micro-loop so the halting op and failure type match per-op interpretation exactly.
    /// </summary>
    [SkipLocalsInit]
    private CallResult RunStream<TCancelable>(
        InstructionStream stream,
        scoped ref SavmStack stack,
        scoped ref TGasPolicy gas)
        where TCancelable : struct, IFlag
    {
        ReturnData = null;
        SavmExceptionType exceptionType = SavmExceptionType.None;
        StreamInterpreter.FramesExecuted++;

        int programCounter = VmState.ProgramCounter;
        delegate*<VirtualMachine<TGasPolicy>, ref SavmStack, ref TGasPolicy, ref int, SavmExceptionType>[] opcodeArray = _opcodeMethods;
        StreamOp[] ops = stream.Ops;
        ulong[] blockGas = stream.BlockGas;
        Int256.UInt256[] constants = stream.Constants;
        byte[] constantBytes = stream.ConstantBytes;
        ushort[] pcToEntry = stream.PcToEntry;
        int callDepth = VmState.Env.CallDepth;
        int opCodeCount = 0;
        bool metered = false;

        // Resume pcs land one past code end at most; the bound guards a truncated trailing PUSH.
        int entryIndex = programCounter == 0
            ? 0
            : (uint)programCounter < (uint)pcToEntry.Length ? pcToEntry[programCounter] : ops.Length;
        fixed (delegate*<VirtualMachine<TGasPolicy>, ref SavmStack, ref TGasPolicy, ref int, SavmExceptionType>* opcodeMethods = &opcodeArray[0])
        {
            while ((uint)entryIndex < (uint)ops.Length)
            {
                ref readonly StreamOp entry = ref ops[entryIndex];
                Instruction instruction = (Instruction)entry.Opcode;

                if (entry.Kind < StreamOpKind.Boundary)
                {
                    if (entry.Kind <= StreamOpKind.FusedBlockFirst)
                    {
                        metered = !TGasPolicy.TryConsume(ref gas, blockGas[entry.BlockIndex]);
                    }

                    if (metered)
                    {
                        // By-value in, struct out: ref params would evict the loop's hot locals from registers.
                        MeteredResult result = RunMeteredSegment<TCancelable>(stream, ref stack, ref gas, entry.Pc, opCodeCount, callDepth);
                        programCounter = result.ProgramCounter;
                        opCodeCount = result.OpCodeCount;
                        entryIndex = result.EntryIndex;
                        metered = result.Metered;
                        exceptionType = result.Exception;
                        if (result.Outcome == MeteredOutcome.Continue)
                            continue;
                        if (result.Outcome == MeteredOutcome.OutOfGas)
                        {
                            OpCodeCount += opCodeCount;
                            goto OutOfGas;
                        }

                        break;
                    }

                    if (TCancelable.IsActive && (opCodeCount & CancellationCheckMask) == 0 && _txTracer.IsCancelled)
                        ThrowStreamOperationCanceledException();

                    TGasPolicy.OnBeforeInstructionTrace(in gas, entry.Pc, instruction, callDepth);
                    opCodeCount += 1 + ((byte)entry.Kind & 1);

                    // Gas already charged at the block entry, so the cores are gas-free. Must stay inline (JIT).
                    switch (instruction)
                    {
                        case (Instruction)FusedOpcode.Add:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpAdd>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.Sub:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpSub>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.Mul:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpMul>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.Div:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpDiv>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.SDiv:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpSDiv>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.Mod:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpMod>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.SMod:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpSMod>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.Lt:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpLt>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.Gt:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpGt>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.SLt:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpSLt>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.SGt:
                            exceptionType = SavmInstructions.FusedConstBinaryCore<SavmInstructions.OpSGt>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.Eq:
                            exceptionType = SavmInstructions.FusedConstBitwiseCore<SavmInstructions.OpBitwiseEq>(ref stack, ref constantBytes[(int)entry.Operand * 32]);
                            break;
                        case (Instruction)FusedOpcode.And:
                            exceptionType = SavmInstructions.FusedConstBitwiseCore<SavmInstructions.OpBitwiseAnd>(ref stack, ref constantBytes[(int)entry.Operand * 32]);
                            break;
                        case (Instruction)FusedOpcode.Or:
                            exceptionType = SavmInstructions.FusedConstBitwiseCore<SavmInstructions.OpBitwiseOr>(ref stack, ref constantBytes[(int)entry.Operand * 32]);
                            break;
                        case (Instruction)FusedOpcode.Xor:
                            exceptionType = SavmInstructions.FusedConstBitwiseCore<SavmInstructions.OpBitwiseXor>(ref stack, ref constantBytes[(int)entry.Operand * 32]);
                            break;
                        case (Instruction)FusedOpcode.Shl:
                            exceptionType = SavmInstructions.FusedConstShiftCore<SavmInstructions.OpShl>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case (Instruction)FusedOpcode.Shr:
                            exceptionType = SavmInstructions.FusedConstShiftCore<SavmInstructions.OpShr>(ref stack, constants[(int)entry.Operand]);
                            break;
                        case Instruction.ADD:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpAdd, OffFlag>(ref stack);
                            break;
                        case Instruction.SUB:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpSub, OffFlag>(ref stack);
                            break;
                        case Instruction.MUL:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpMul, OffFlag>(ref stack);
                            break;
                        case Instruction.DIV:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpDiv, OffFlag>(ref stack);
                            break;
                        case Instruction.SDIV:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpSDiv, OffFlag>(ref stack);
                            break;
                        case Instruction.MOD:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpMod, OffFlag>(ref stack);
                            break;
                        case Instruction.SMOD:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpSMod, OffFlag>(ref stack);
                            break;
                        case Instruction.LT:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpLt, OffFlag>(ref stack);
                            break;
                        case Instruction.GT:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpGt, OffFlag>(ref stack);
                            break;
                        case Instruction.SLT:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpSLt, OffFlag>(ref stack);
                            break;
                        case Instruction.SGT:
                            exceptionType = SavmInstructions.Math2ParamCore<SavmInstructions.OpSGt, OffFlag>(ref stack);
                            break;
                        case Instruction.EQ:
                            exceptionType = SavmInstructions.BitwiseCore<SavmInstructions.OpBitwiseEq>(ref stack);
                            break;
                        case Instruction.AND:
                            exceptionType = SavmInstructions.BitwiseCore<SavmInstructions.OpBitwiseAnd>(ref stack);
                            break;
                        case Instruction.OR:
                            exceptionType = SavmInstructions.BitwiseCore<SavmInstructions.OpBitwiseOr>(ref stack);
                            break;
                        case Instruction.XOR:
                            exceptionType = SavmInstructions.BitwiseCore<SavmInstructions.OpBitwiseXor>(ref stack);
                            break;
                        case Instruction.ISZERO:
                            exceptionType = SavmInstructions.Math1ParamCore<SavmInstructions.OpIsZero>(ref stack);
                            break;
                        case Instruction.NOT:
                            exceptionType = SavmInstructions.Math1ParamCore<SavmInstructions.OpNot>(ref stack);
                            break;
                        case Instruction.SHL:
                            exceptionType = SavmInstructions.ShiftCore<SavmInstructions.OpShl, OffFlag>(ref stack);
                            break;
                        case Instruction.SHR:
                            exceptionType = SavmInstructions.ShiftCore<SavmInstructions.OpShr, OffFlag>(ref stack);
                            break;
                        case Instruction.POP:
                            exceptionType = stack.PopLimbo() ? SavmExceptionType.None : SavmExceptionType.StackUnderflow;
                            break;
                        case Instruction.PUSH0:
                            exceptionType = stack.PushZero<OffFlag>();
                            break;
                        case Instruction.PUSH1:
                        case >= Instruction.PUSH3 and <= Instruction.PUSH8:
                            // Analyzer pre-decoded full-width immediates; a truncated trailing PUSH stays a boundary op.
                            exceptionType = stack.PushUInt64<OffFlag>(entry.Operand);
                            break;
                        case >= Instruction.PUSH9 and <= Instruction.PUSH32:
                            exceptionType = stack.PushUInt256<OffFlag>(in constants[(int)entry.Operand]);
                            break;
                        case >= Instruction.DUP1 and <= Instruction.DUP8:
                            exceptionType = stack.Dup<OffFlag>(instruction - Instruction.DUP1 + 1);
                            break;
                        case >= Instruction.SWAP1 and <= Instruction.SWAP8:
                            exceptionType = stack.Swap<OffFlag>(instruction - Instruction.SWAP1 + 2);
                            break;
                        case Instruction.JUMPDEST:
                            exceptionType = SavmExceptionType.None;
                            break;
                        case (Instruction)FusedOpcode.StaticJump:
                            // PUSH2 + JUMP, JUMPDEST validated at analysis; self-charges since outside any block.
                            // Mirror the unfused PUSH2: at a full stack the PUSH2 overflows before the JUMP would
                            // pop it, so the fast path must fail with StackOverflow rather than execute the jump.
                            if (stack.Head >= SavmStack.MaxStackSize - 1)
                            {
                                exceptionType = SavmExceptionType.StackOverflow;
                                break;
                            }
                            if (!TGasPolicy.TryConsume(ref gas, GasCostOf.VeryLow + GasCostOf.Jump))
                            {
                                TGasPolicy.SetOutOfGas(ref gas);
                                OpCodeCount += opCodeCount;
                                goto OutOfGas;
                            }

                            opCodeCount++;
                            // Set entryIndex to dest-1; the shared loop-tail entryIndex++ lands it on dest.
                            // programCounter is transiently stale until the next entry sets it — fine, nothing reads it here.
                            entryIndex = (int)entry.Operand - 1;
                            break;
                        case (Instruction)FusedOpcode.StaticJumpI:
                            // Same full-stack overflow as the unfused PUSH2, which pushes the destination before
                            // JUMPI pops it; fail with StackOverflow instead of testing the condition and jumping.
                            if (stack.Head >= SavmStack.MaxStackSize - 1)
                            {
                                exceptionType = SavmExceptionType.StackOverflow;
                                break;
                            }
                            if (!TGasPolicy.TryConsume(ref gas, GasCostOf.VeryLow + GasCostOf.JumpI))
                            {
                                TGasPolicy.SetOutOfGas(ref gas);
                                OpCodeCount += opCodeCount;
                                goto OutOfGas;
                            }

                            if (SavmInstructions.TestJumpCondition(ref stack, out bool conditionUnderflow))
                            {
                                entryIndex = (int)entry.Operand - 1;
                            }
                            else if (conditionUnderflow)
                            {
                                exceptionType = SavmExceptionType.StackUnderflow;
                            }

                            break;
                        default:
                            // Unreachable: every in-block opcode has a case above. Fail closed rather than
                            // mis-dispatch a precharged op and corrupt gas.
                            exceptionType = SavmExceptionType.BadInstruction;
                            break;
                    }

                    if (exceptionType != SavmExceptionType.None)
                        break;

                    programCounter = entry.Pc + entry.Advance;
                    entryIndex++;
                    continue;
                }

                // Fast path for single-byte, dynamic-gas memory ops: they never redirect control flow,
                // so direct dispatch (no table calli) + sequential advance (no pcToEntry landing
                // recompute). The general boundary epilogue below is pure overhead for these cheap ops
                // and dominated them — a boundary op always resets the open block, so metered stays off.
                if (instruction is Instruction.MSTORE or Instruction.MLOAD or Instruction.MCOPY)
                {
                    if (TCancelable.IsActive && (opCodeCount & CancellationCheckMask) == 0 && _txTracer.IsCancelled)
                        ThrowStreamOperationCanceledException();

                    TGasPolicy.OnBeforeInstructionTrace(in gas, entry.Pc, instruction, callDepth);
                    opCodeCount++;

                    int mpc = entry.Pc + 1;
                    exceptionType = instruction switch
                    {
                        Instruction.MSTORE => SavmInstructions.InstructionMStore<TGasPolicy, OffFlag>(this, ref stack, ref gas, ref mpc),
                        Instruction.MLOAD => SavmInstructions.InstructionMLoad<TGasPolicy, OffFlag>(this, ref stack, ref gas, ref mpc),
                        _ => SavmInstructions.InstructionMCopy<TGasPolicy, OffFlag>(this, ref stack, ref gas, ref mpc),
                    };

                    if (TGasPolicy.IsOutOfGas(in gas))
                    {
                        OpCodeCount += opCodeCount;
                        goto OutOfGas;
                    }

                    TGasPolicy.OnAfterInstructionTrace(in gas);
                    if (exceptionType != SavmExceptionType.None) break;

                    metered = false;
                    programCounter = entry.Pc + entry.Advance;
                    entryIndex++;
                    continue;
                }

                // Boundary op: standard handler + epilogue. Structured control flow only —
                // backward gotos make the loop irreducible and the JIT stops optimizing it.
                programCounter = entry.Pc;
                if (TCancelable.IsActive && (opCodeCount & CancellationCheckMask) == 0 && _txTracer.IsCancelled)
                    ThrowStreamOperationCanceledException();

                TGasPolicy.OnBeforeInstructionTrace(in gas, programCounter, instruction, callDepth);
                programCounter++;
                opCodeCount++;

                // Stack temp by ref keeps programCounter register-resident across the loop.
                int pc = programCounter;
                exceptionType = opcodeMethods[(int)instruction](this, ref stack, ref gas, ref pc);
                programCounter = pc;

                if (TGasPolicy.IsOutOfGas(in gas))
                {
                    OpCodeCount += opCodeCount;
                    goto OutOfGas;
                }

                TGasPolicy.OnAfterInstructionTrace(in gas);

                if (exceptionType != SavmExceptionType.None)
                    break;

                if (ReturnData is not null)
                    break;

                // Table handlers may consume more than one instruction; recompute the entry from the landing pc.
                if ((uint)programCounter >= (uint)pcToEntry.Length)
                {
                    entryIndex = ops.Length;
                    continue;
                }

                int landing = pcToEntry[programCounter];
                if (landing == InstructionStream.InvalidEntry)
                {
                    // Nothing may land between entries — fail loudly rather than silently succeed.
                    exceptionType = SavmExceptionType.InvalidJumpDestination;
                    break;
                }

                entryIndex = landing;
                if ((uint)entryIndex < (uint)ops.Length)
                {
                    // A fused table handler can land past a block's charging entry; run the rest metered.
                    StreamOpKind landingKind = ops[entryIndex].Kind;
                    metered = landingKind is StreamOpKind.InBlock or StreamOpKind.FusedInBlock;
                }
            }

            OpCodeCount += opCodeCount;
        }

        if (exceptionType is SavmExceptionType.None or SavmExceptionType.Stop or SavmExceptionType.Revert)
        {
            UpdateCurrentState(programCounter, in gas, stack.Head);
        }
        else
        {
            goto ReturnFailure;
        }

        if (exceptionType == SavmExceptionType.Revert)
            goto Revert;
        if (ReturnData is not null)
            goto DataReturn;

        return CallResult.Empty();

    DataReturn:
        if (ReturnData is byte[] data)
        {
            return new CallResult(data, null);
        }
        else if (ReturnData is VmState<TGasPolicy> state)
        {
            return new CallResult(state);
        }
        return new CallResult(ReturnDataBuffer, null);

    Revert:
        return new CallResult((byte[])ReturnData, null, shouldRevert: true, exceptionType);

    OutOfGas:
        TGasPolicy.SetOutOfGas(ref gas);
        exceptionType = SavmExceptionType.OutOfGas;
    ReturnFailure:
        _currentState.Gas = gas;
        return GetFailureReturn(TGasPolicy.GetRemainingGas(in gas), exceptionType);

        [DoesNotReturn]
        static void ThrowStreamOperationCanceledException() => throw new OperationCanceledException("Cancellation Requested");
    }

    private enum MeteredOutcome : byte
    {
        Continue,
        BreakLoop,
        OutOfGas,
    }

    private readonly record struct MeteredResult(
        MeteredOutcome Outcome,
        int ProgramCounter,
        int OpCodeCount,
        int EntryIndex,
        bool Metered,
        SavmExceptionType Exception);

    /// <summary>
    /// Cold path: per-op metered execution over raw code (exact gas and failure semantics) for a block whose
    /// precharge didn't fit. Kept out of <see cref="RunStream{TCancelable}"/> so the hot loop stays small and reducible.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private MeteredResult RunMeteredSegment<TCancelable>(
        InstructionStream stream,
        scoped ref SavmStack stack,
        scoped ref TGasPolicy gas,
        int programCounter,
        int opCodeCount,
        int callDepth)
        where TCancelable : struct, IFlag
    {
        int entryIndex = 0;
        bool metered = true;
        SavmExceptionType exceptionType = SavmExceptionType.None;
        StreamOp[] ops = stream.Ops;
        ushort[] pcToEntry = stream.PcToEntry;
        ref byte code = ref stack.Code;
        uint codeLength = (uint)stack.CodeLength;
        delegate*<VirtualMachine<TGasPolicy>, ref SavmStack, ref TGasPolicy, ref int, SavmExceptionType>[] opcodeMethods = _opcodeMethods;

        while (true)
        {
            if ((uint)programCounter >= codeLength)
            {
                return new MeteredResult(MeteredOutcome.Continue, programCounter, opCodeCount, ops.Length, metered, exceptionType);
            }

            Instruction instruction = (Instruction)Unsafe.Add(ref code, programCounter);

            if (TCancelable.IsActive && (opCodeCount & CancellationCheckMask) == 0 && _txTracer.IsCancelled)
                throw new OperationCanceledException("Cancellation Requested");

            TGasPolicy.OnBeforeInstructionTrace(in gas, programCounter, instruction, callDepth);
            programCounter++;
            opCodeCount++;

            exceptionType = opcodeMethods[(int)instruction](this, ref stack, ref gas, ref programCounter);

            if (TGasPolicy.IsOutOfGas(in gas))
                return new MeteredResult(MeteredOutcome.OutOfGas, programCounter, opCodeCount, entryIndex, metered, exceptionType);

            TGasPolicy.OnAfterInstructionTrace(in gas);

            if (exceptionType != SavmExceptionType.None)
                return new MeteredResult(MeteredOutcome.BreakLoop, programCounter, opCodeCount, entryIndex, metered, exceptionType);

            if (ReturnData is not null)
                return new MeteredResult(MeteredOutcome.BreakLoop, programCounter, opCodeCount, entryIndex, metered, exceptionType);

            if ((uint)programCounter >= (uint)pcToEntry.Length)
            {
                return new MeteredResult(MeteredOutcome.Continue, programCounter, opCodeCount, ops.Length, metered, exceptionType);
            }

            int landing = pcToEntry[programCounter];
            if (landing == InstructionStream.InvalidEntry)
                continue; // interior pc: keep stepping raw code

            entryIndex = landing;
            if ((uint)entryIndex >= (uint)ops.Length)
                return new MeteredResult(MeteredOutcome.Continue, programCounter, opCodeCount, entryIndex, metered, exceptionType);

            StreamOpKind kind = ops[entryIndex].Kind;
            if (kind is StreamOpKind.InBlock or StreamOpKind.FusedInBlock)
                continue;

            // Block-charging entry or boundary op: hand back so the stream loop re-evaluates the charge.
            return new MeteredResult(MeteredOutcome.Continue, programCounter, opCodeCount, entryIndex, Metered: false, Exception: exceptionType);
        }
    }
}
