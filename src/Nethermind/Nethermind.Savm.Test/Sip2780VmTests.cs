// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Savm.State;
using Nethermind.Specs;
using Nethermind.Specs.Forks;
using Nethermind.Specs.Test;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

/// <summary>
/// Opcode-level SIP-2780 coverage: each test diffs two runs so the identical intrinsic and
/// recipient costs cancel, isolating the SIP-2780 delta.
/// </summary>
public class Sip2780VmTests : VirtualMachineTestsBase
{
    protected override ISpecProvider SpecProvider { get; } =
        new TestSpecProvider(new OverridableReleaseSpec(SilaPrague.Instance) { IsSip2780Enabled = true, IsSip7708Enabled = true });

    private ulong GasSpent(byte[] code)
    {
        TestAllTracerWithOutput result = Execute(code);
        Assert.That(result.StatusCode, Is.EqualTo(StatusCode.Success), result.Error);
        return result.GasSpent;
    }

    // Gas charged at the given opcode's step, per the Geth-style trace.
    private ulong OpCost(string opcode, byte[] code)
    {
        foreach (global::Nethermind.Blockchain.Tracing.GethStyle.GethTxTraceEntry e in ExecuteAndTrace(code).Entries)
        {
            if (e.Opcode == opcode) return e.GasCost;
        }
        return ulong.MaxValue;
    }

    [Test]
    public void Cold_account_access_via_balance_is_flat()
    {
        Address codeless = TestItem.AddressC;
        Address withCode = TestItem.AddressF;
        TestState.CreateAccount(codeless, 1.Sila);
        TestState.CreateAccount(withCode, 1.Sila);
        TestState.InsertCode(withCode, Prepare.SavmCode.Op(Instruction.STOP).Done, Spec);

        ulong codelessCost = OpCost("BALANCE", Prepare.SavmCode.PushData(codeless).Op(Instruction.BALANCE).STOP().Done);
        ulong withCodeCost = OpCost("BALANCE", Prepare.SavmCode.PushData(withCode).Op(Instruction.BALANCE).STOP().Done);

        Assert.That((codelessCost, withCodeCost), Is.EqualTo((GasCostOf.ColdAccountAccess, GasCostOf.ColdAccountAccess)));
    }

    [Test]
    public void Call_value_cost_is_independent_of_recipient_existence()
    {
        Address existing = TestItem.AddressC;
        Address newAccount = TestItem.AddressF; // dead account: no surcharge, cost is state-independent
        TestState.CreateAccount(existing, 1.Sila);

        ulong existingGas = GasSpent(Prepare.SavmCode.CallWithValue(existing, 50000, 1).STOP().Done);
        ulong newAccountGas = GasSpent(Prepare.SavmCode.CallWithValue(newAccount, 50000, 1).STOP().Done);

        Assert.That(newAccountGas, Is.EqualTo(existingGas));
    }

    [Test]
    public void Callcode_with_value_charges_flat_call_value()
    {
        Address codeSource = TestItem.AddressC;
        TestState.CreateAccount(codeSource, 1.Sila);
        TestState.InsertCode(codeSource, Prepare.SavmCode.Op(Instruction.STOP).Done, Spec);

        ulong noValueOp = OpCost("CALLCODE", Prepare.SavmCode.CallCode(codeSource, 50000, 0).STOP().Done);
        ulong withValueOp = OpCost("CALLCODE", Prepare.SavmCode.CallCode(codeSource, 50000, 1).STOP().Done);

        Assert.That(withValueOp - noValueOp, Is.EqualTo(Sip8038Constants.CallValue));
    }

    [Test]
    public void Delegated_recipient_costs_the_same_as_plain_contract()
    {
        // The flat intrinsic already covers the recipient; the delegation-target top-frame
        // charge only exists under SIP-8037 (see Sip8037RegressionTests).
        Address target = TestItem.AddressC;
        TestState.CreateAccount(target, 1.Sila);
        TestState.InsertCode(target, Prepare.SavmCode.Op(Instruction.STOP).Done, Spec);
        byte[] delegated = [.. Sip7702Constants.DelegationHeader, .. target.Bytes];

        ulong delegatedGas = GasSpent(delegated);
        ulong plainContractGas = GasSpent(Prepare.SavmCode.Op(Instruction.STOP).Done);

        Assert.That(delegatedGas, Is.EqualTo(plainContractGas));
    }
}
