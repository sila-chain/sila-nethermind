// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Nethermind.Core;
using Nethermind.Savm.Tracing;
#if DEBUG
using Nethermind.Savm.Tracing.Debugger;
#endif

namespace Nethermind.Savm;

/// <summary>
/// The interpreter dispatch loop. A direct <c>switch</c> over the hot opcodes (so they dispatch without the
/// function-pointer <c>calli</c> and the JIT can inline the handler bodies); every other opcode falls through
/// to the per-fork function-pointer table. Opcode-availability gates are lifted to compile-time <see cref="IFlag"/>
/// type args (<c>TShift</c> for SIP-145 SHL/SHR, <c>TPush0</c> for SIP-3855 PUSH0) so the JIT folds them and
/// drops the untaken cases. Every other hot opcode is fork-invariant, so one loop body serves all forks.
/// </summary>
public unsafe partial class VirtualMachine<TGasPolicy>
{
    // Poll cancellation every 1024 opcodes (low bits of the per-frame op counter).
    private const int CancellationCheckMask = 1023;

    [SkipLocalsInit]
    private CallResult RunByteCodeCore<TTracingInst, TCancelable, TShift, TPush0>(
        scoped ref SavmStack stack,
        scoped ref TGasPolicy gas)
        where TTracingInst : struct, IFlag
        where TCancelable : struct, IFlag
        where TShift : struct, IFlag
        where TPush0 : struct, IFlag
    {
        ReturnData = null;
        SavmExceptionType exceptionType = SavmExceptionType.None;
#if DEBUG
        DebugTracer<TGasPolicy>? debugger = _txTracer.GetTracer<DebugTracer<TGasPolicy>>();
#endif

        // May not be zero when resuming after a call.
        int programCounter = VmState.ProgramCounter;
        // Pinned pointer drops the per-dispatch bounds check (opcode is a byte, always in range).
        delegate*<VirtualMachine<TGasPolicy>, ref SavmStack, ref TGasPolicy, ref int, SavmExceptionType>[] opcodeArray = _opcodeMethods;
        fixed (delegate*<VirtualMachine<TGasPolicy>, ref SavmStack, ref TGasPolicy, ref int, SavmExceptionType>* opcodeMethods = &opcodeArray[0])
        {
            int opCodeCount = 0;
            ref Instruction code = ref Unsafe.As<byte, Instruction>(ref stack.Code);
            uint codeLength = (uint)stack.CodeLength;
            // Hoisted: a no-op OnBeforeInstructionTrace would otherwise chase VmState.Env per instruction.
            int callDepth = VmState.Env.CallDepth;
            while ((uint)programCounter < codeLength)
            {
#if DEBUG
                debugger?.TryWait(ref _currentState, ref programCounter, ref gas, ref stack.Head);
#endif
                Instruction instruction = Unsafe.Add(ref code, programCounter);

                // IsCancelled is an interface call; polling it per opcode is measurable on the
                // cancelable (sil_call) path. Every 1024 opcodes still aborts within microseconds.
                if (TCancelable.IsActive && (opCodeCount & CancellationCheckMask) == 0 && _txTracer.IsCancelled)
                    ThrowOperationCanceledException();

                TGasPolicy.OnBeforeInstructionTrace(in gas, programCounter, instruction, callDepth);

                if (TTracingInst.IsActive)
                    StartInstructionTrace(instruction, TGasPolicy.GetRemainingGas(in gas), programCounter, in stack);

                programCounter++;
                opCodeCount++;

                // Stack temp by ref keeps programCounter register-resident; passing ref programCounter to the
                // handlers (incl. the calli) address-takes it, forcing a frame reload every opcode.
                int pc = programCounter;
                bool directDispatch =
#if ZK_SAVM
                // The guest has no I-cache and counts executed instructions, so inlining the hot handlers
                // is a win on every path; it always direct-dispatches, regardless of cancelability.
                true;
#else
                // The direct-dispatch switch inlines the hot handlers and pays off only for the cancelable
                // (sil_call/simulation) path, where a few hot contracts run repeatedly and stay in I-cache.
                // Block processing runs a diverse opcode mix across many contracts, where the switch's
                // jump-table indirection and code-size pressure measurably regress throughput versus the
                // plain function-pointer table; that path takes the table below.
                TCancelable.IsActive;
#endif
                // directDispatch folds at compile time, so this specializes into two loop bodies with no runtime branch.
                if (directDispatch)
                {
                    // Direct dispatch for the measured-hot opcodes; the rest take the table. MUST stay inline:
                    // extracted, the JIT stops inlining the handlers and direct dispatch loses to the table's calli.
                    switch (instruction)
                    {
                        case Instruction.ADD:
                            exceptionType = SavmInstructions.InstructionMath2Param<TGasPolicy, SavmInstructions.OpAdd, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.SUB:
                            exceptionType = SavmInstructions.InstructionMath2Param<TGasPolicy, SavmInstructions.OpSub, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.MUL:
                            exceptionType = SavmInstructions.InstructionMath2Param<TGasPolicy, SavmInstructions.OpMul, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.LT:
                            exceptionType = SavmInstructions.InstructionMath2Param<TGasPolicy, SavmInstructions.OpLt, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.GT:
                            exceptionType = SavmInstructions.InstructionMath2Param<TGasPolicy, SavmInstructions.OpGt, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.EQ:
                            exceptionType = SavmInstructions.InstructionBitwise<TGasPolicy, SavmInstructions.OpBitwiseEq>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.ISZERO:
                            exceptionType = SavmInstructions.InstructionMath1Param<TGasPolicy, SavmInstructions.OpIsZero>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.AND:
                            exceptionType = SavmInstructions.InstructionBitwise<TGasPolicy, SavmInstructions.OpBitwiseAnd>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.OR:
                            exceptionType = SavmInstructions.InstructionBitwise<TGasPolicy, SavmInstructions.OpBitwiseOr>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.NOT:
                            exceptionType = SavmInstructions.InstructionMath1Param<TGasPolicy, SavmInstructions.OpNot>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.SHL:
                            if (!TShift.IsActive) goto default;
                            exceptionType = SavmInstructions.InstructionShift<TGasPolicy, SavmInstructions.OpShl, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.SHR:
                            if (!TShift.IsActive) goto default;
                            exceptionType = SavmInstructions.InstructionShift<TGasPolicy, SavmInstructions.OpShr, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.CALLDATALOAD:
                            exceptionType = SavmInstructions.InstructionCallDataLoad<TGasPolicy, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.MLOAD:
                            exceptionType = SavmInstructions.InstructionMLoad<TGasPolicy, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.MSTORE:
                            exceptionType = SavmInstructions.InstructionMStore<TGasPolicy, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.SLOAD:
                            exceptionType = SavmInstructions.InstructionSLoad<TGasPolicy, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.JUMP:
                            exceptionType = SavmInstructions.InstructionJump(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.JUMPI:
                            exceptionType = SavmInstructions.InstructionJumpIf(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.JUMPDEST:
                            exceptionType = SavmInstructions.InstructionJumpDest(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.POP:
                            exceptionType = SavmInstructions.InstructionPop(this, ref stack, ref gas, ref pc);
                            break;
#if ZK_SAVM
                        // GAS is hot on the guest but absent from mainline's curated sil_call set.
                        case Instruction.GAS:
                            exceptionType = SavmInstructions.InstructionGas<TGasPolicy, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
#endif
                        case Instruction.PUSH0:
                            if (!TPush0.IsActive) goto default;
                            exceptionType = SavmInstructions.InstructionPush0<TGasPolicy, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.PUSH1:
                            exceptionType = SavmInstructions.InstructionPush<TGasPolicy, SavmInstructions.Op1, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.PUSH2:
                            exceptionType = SavmInstructions.InstructionPush2<TGasPolicy, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.PUSH3:
                            exceptionType = SavmInstructions.InstructionPush<TGasPolicy, SavmInstructions.Op3, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.PUSH4:
                            exceptionType = SavmInstructions.InstructionPush<TGasPolicy, SavmInstructions.Op4, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.DUP1:
                            exceptionType = SavmInstructions.InstructionDup<TGasPolicy, SavmInstructions.Op1, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.DUP2:
                            exceptionType = SavmInstructions.InstructionDup<TGasPolicy, SavmInstructions.Op2, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.DUP3:
                            exceptionType = SavmInstructions.InstructionDup<TGasPolicy, SavmInstructions.Op3, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.DUP4:
                            exceptionType = SavmInstructions.InstructionDup<TGasPolicy, SavmInstructions.Op4, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.DUP5:
                            exceptionType = SavmInstructions.InstructionDup<TGasPolicy, SavmInstructions.Op5, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.SWAP1:
                            exceptionType = SavmInstructions.InstructionSwap<TGasPolicy, SavmInstructions.Op1, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.SWAP2:
                            exceptionType = SavmInstructions.InstructionSwap<TGasPolicy, SavmInstructions.Op2, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        case Instruction.SWAP3:
                            exceptionType = SavmInstructions.InstructionSwap<TGasPolicy, SavmInstructions.Op3, TTracingInst>(this, ref stack, ref gas, ref pc);
                            break;
                        default:
                            exceptionType = opcodeMethods[(int)instruction](this, ref stack, ref gas, ref pc);
                            break;
                    }
                }
                else
                {
                    // Block-processing path: plain function-pointer table (master-parity dispatch), with POP
                    // kept inline as the single measurably-hot, fork-invariant special case.
                    if (Instruction.POP == instruction)
                        exceptionType = SavmInstructions.InstructionPop(this, ref stack, ref gas, ref pc);
                    else
                        exceptionType = opcodeMethods[(int)instruction](this, ref stack, ref gas, ref pc);
                }
                programCounter = pc;

                if (TGasPolicy.IsOutOfGas(in gas))
                {
                    OpCodeCount += opCodeCount;
                    goto OutOfGas;
                }

                TGasPolicy.OnAfterInstructionTrace(in gas);

                if (exceptionType != SavmExceptionType.None)
                    break;

                if (TTracingInst.IsActive)
                    EndInstructionTrace(TGasPolicy.GetRemainingGas(in gas));

                // Only the 0xF0+ family sets ReturnData (RETURN returns None and signals completion solely
                // through it), so the field load is skipped for the cheap majority below CREATE.
                if (instruction >= Instruction.CREATE && ReturnData is not null)
                {
                    break;
                }
            }

            OpCodeCount += opCodeCount;
        }

        if (exceptionType is SavmExceptionType.None or SavmExceptionType.Stop or SavmExceptionType.Revert)
        {
            if (TTracingInst.IsActive)
                EndInstructionTrace(TGasPolicy.GetRemainingGas(in gas));
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

#if DEBUG
        debugger?.TryWait(ref _currentState, ref programCounter, ref gas, ref stack.Head);
#endif
        return CallResult.Empty();

    DataReturn:
#if DEBUG
        debugger?.TryWait(ref _currentState, ref programCounter, ref gas, ref stack.Head);
#endif
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
        // SIP-8037: write gas back to state on failure so RestoreChildStateGasOnHalt can read
        // accumulated StateGasUsed/StateGasSpill from the child frame.
        _currentState.Gas = gas;
        return GetFailureReturn(TGasPolicy.GetRemainingGas(in gas), exceptionType);

        [DoesNotReturn]
        static void ThrowOperationCanceledException() => throw new OperationCanceledException("Cancellation Requested");
    }
}
