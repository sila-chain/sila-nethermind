// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Specs;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

public class StackUnderflowRegressionTests : VirtualMachineTestsBase
{
    protected override ulong BlockNumber => MainnetSpecProvider.ParisBlockNumber;
    protected override ulong Timestamp => MainnetSpecProvider.CancunBlockTimestamp;

    // Each case leaves the stack exactly one item short of what the opcode's converted pop needs:
    // the preceding pops succeed, then the value/topic/salt pop underflows.
    private static readonly object[] UnderflowCases =
    [
        new object[] { Instruction.BYTE, Prepare.SavmCode.PushData(0).Op(Instruction.BYTE).Done },
        new object[] { Instruction.SSTORE, Prepare.SavmCode.PushData(0).Op(Instruction.SSTORE).Done },
        new object[] { Instruction.TSTORE, Prepare.SavmCode.PushData(0).Op(Instruction.TSTORE).Done },
        new object[] { Instruction.LOG1, Prepare.SavmCode.PushData(0).PushData(0).Op(Instruction.LOG1).Done },
        new object[] { Instruction.CREATE2, Prepare.SavmCode.PushData(0).PushData(0).PushData(0).Op(Instruction.CREATE2).Done },
        // Index 0 is in-range, so SIGNEXTEND skips the out-of-range short-circuit and peeks the missing value.
        new object[] { Instruction.SIGNEXTEND, Prepare.SavmCode.PushData(0).Op(Instruction.SIGNEXTEND).Done },
    ];

    [TestCaseSource(nameof(UnderflowCases))]
    public void Signals_stack_underflow_when_final_operand_missing(Instruction opcode, byte[] code)
    {
        TestAllTracerWithOutput result = Execute(code);
        Assert.That(result.Error, Is.EqualTo(SavmExceptionType.StackUnderflow.ToString()), opcode.ToString());
    }
}
