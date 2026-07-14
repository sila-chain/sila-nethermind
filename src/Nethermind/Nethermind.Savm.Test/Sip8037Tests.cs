// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using Nethermind.Core;
using Nethermind.Core.Sip2930;
using Nethermind.Core.Test.Builders;
using Nethermind.Savm.CodeAnalysis;
using Nethermind.Savm.GasPolicy;
using Nethermind.Savm.State;
using Nethermind.Savm.Tracing;
using Nethermind.Int256;
using Nethermind.Specs;
using Nethermind.Specs.Forks;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

public class Sip8037Tests : VirtualMachineTestsBase
{
    protected override ulong BlockNumber => MainnetSpecProvider.ParisBlockNumber;
    protected override ulong Timestamp => MainnetSpecProvider.AmsterdamBlockTimestamp;

    private static IEnumerable<TestCaseData> ConstantsTestCases()
    {
        yield return new TestCaseData(GasCostOf.CostPerStateByte).Returns(1530L).SetName("CostPerStateByte");
        yield return new TestCaseData(GasCostOf.SSetState).Returns(97920L).SetName("SSetState");
        yield return new TestCaseData(GasCostOf.CreateState).Returns(183600L).SetName("CreateState");
        yield return new TestCaseData(GasCostOf.NewAccountState).Returns(183600L).SetName("NewAccountState");
        yield return new TestCaseData(GasCostOf.PerAuthBaseState).Returns(35190L).SetName("PerAuthBaseState");
        yield return new TestCaseData(Sip8037Constants.SystemCallStateReservoir).Returns(1566720L).SetName("SystemCallStateReservoir");
        yield return new TestCaseData((long)Sip8037Constants.SystemCallGasLimit).Returns(31566720L).SetName("SystemCallGasLimit");
    }

    [TestCaseSource(nameof(ConstantsTestCases))]
    public long Constants_are_calculated_correctly(long actual) => actual;

    [TestCase(1, ExpectedResult = 6ul)]
    [TestCase(32, ExpectedResult = 6ul)]
    [TestCase(33, ExpectedResult = 12ul)]
    public ulong Code_deposit_regular_cost(int codeLength)
    {
        CodeDepositHandler.CalculateCost(SilaAmsterdam.Instance, codeLength, out ulong regularCost, out _);
        return regularCost;
    }

    [TestCase(1, ExpectedResult = 1530L)]
    [TestCase(32, ExpectedResult = 48960L)]
    [TestCase(33, ExpectedResult = 50490L)]
    public long Code_deposit_state_cost(int codeLength)
    {
        CodeDepositHandler.CalculateCost(SilaAmsterdam.Instance, codeLength, out _, out long stateCost);
        return stateCost;
    }

    [Test]
    public void System_transaction_gas_keeps_regular_budget_and_state_reservoir()
    {
        SilaGasPolicy intrinsicGas = SilaGasPolicy.CreateSystemTransactionIntrinsicGas(Sip8037Constants.SystemCallGasLimit);
        SilaGasPolicy availableGas = SilaGasPolicy.CreateSystemTransactionAvailableGas(Sip8037Constants.SystemCallGasLimit, in intrinsicGas, SilaAmsterdam.Instance);

        Assert.That(
            (
                intrinsicGas.Value,
                intrinsicGas.StateReservoir,
                intrinsicGas.StateGasUsed,
                availableGas.Value,
                availableGas.StateReservoir,
                availableGas.StateGasUsed
            ),
            Is.EqualTo(
                (
                    0L,
                    Sip8037Constants.SystemCallStateReservoir,
                    0L,
                    Sip8037Constants.SystemCallBaseGasLimit,
                    Sip8037Constants.SystemCallStateReservoir,
                    0L
                )));

        for (ulong i = 0ul; i < Sip8037Constants.SystemMaxSstoresPerCall; i++)
        {
            Assert.That(SilaGasPolicy.ConsumeStateGas(ref availableGas, GasCostOf.SSetState), Is.True);
        }

        Assert.That(
            (
                availableGas.Value,
                availableGas.StateReservoir,
                availableGas.StateGasUsed
            ),
            Is.EqualTo(
                (
                    Sip8037Constants.SystemCallBaseGasLimit,
                    0L,
                    Sip8037Constants.SystemCallStateReservoir
                )));
    }

    [Test]
    public void Regular_transaction_gas_uses_tx_cap_even_when_intrinsic_state_matches_system_reservoir()
    {
        SilaGasPolicy intrinsicGas = new()
        {
            StateReservoir = Sip8037Constants.SystemCallStateReservoir,
        };

        SilaGasPolicy availableGas = SilaGasPolicy.CreateAvailableFromIntrinsic(Sip8037Constants.SystemCallGasLimit, in intrinsicGas, SilaAmsterdam.Instance);
        long expectedReservoir = (long)(Sip8037Constants.SystemCallGasLimit - Sip8037Constants.SystemCallStateReservoir - Sip7825Constants.DefaultTxGasLimitCap);

        Assert.That(
            (
                availableGas.Value,
                availableGas.StateReservoir,
                availableGas.StateGasUsed
            ),
            Is.EqualTo(
                (
                    Sip7825Constants.DefaultTxGasLimitCap,
                    expectedReservoir,
                    Sip8037Constants.SystemCallStateReservoir
                )));
    }

    [Test]
    public void Generic_code_deposit_cost_uses_fixed_state_pricing()
    {
        SilaGasPolicy gas = default;

        bool success = CodeDepositHandler.CalculateCost(SilaAmsterdam.Instance, 33, in gas, out ulong regularCost, out long stateCost);

        Assert.That((success, regularCost, stateCost),
            Is.EqualTo((true, 12L, GasCostOf.CodeDepositState * 33)));
    }

    [Test]
    public void Intrinsic_gas_uses_fixed_state_costs()
    {
        Transaction tx = Build.A.Transaction.SignedAndResolved()
            .WithAuthorizationCode(new AuthorizationTuple(1, TestItem.AddressF, 0, 0, UInt256.One, UInt256.One))
            .TestObject;

        IntrinsicGas<SilaGasPolicy> intrinsicGas = SilaGasPolicy.CalculateIntrinsicGas(tx, SilaAmsterdam.Instance, 30_000_000);

        Assert.That(intrinsicGas.Standard.StateReservoir,
            Is.EqualTo(GasCostOf.NewAccountState + GasCostOf.PerAuthBaseState));
    }

    [Test]
    public void Amsterdam_access_list_floor_pricing_is_added_to_regular_and_floor_intrinsic_gas()
    {
        AccessList accessList = new AccessList.Builder()
            .AddAddress(TestItem.AddressA)
            .AddStorage(UInt256.One)
            .AddStorage((UInt256)2)
            .AddStorage((UInt256)3)
            .Build();
        Transaction tx = Build.A.Transaction.SignedAndResolved()
            .WithAccessList(accessList)
            .TestObject;

        IntrinsicGas<SilaGasPolicy> splitIntrinsicGas = SilaGasPolicy.CalculateIntrinsicGas(tx, SilaAmsterdam.Instance);
        SilaIntrinsicGas intrinsicGas = IntrinsicGasCalculator.Calculate(tx, SilaAmsterdam.Instance);
        // Access-list entries repriced to the cold costs; the value-bearing recipient touch
        // adds COLD_ACCOUNT_ACCESS + TRANSFER_LOG + TX_VALUE.
        ulong recipientRegular = Sip8038Constants.ColdAccountAccess + GasCostOf.TransferLogSip2780 + GasCostOf.TxValueCostSip2780;
        ulong accessListBaseCost = Sip8038Constants.AccessListAddressCost + 3 * Sip8038Constants.AccessListStorageKeyCost;
        ulong accessListFloorTokens = (20ul + 3ul * 32ul) * SilaAmsterdam.Instance.GasCosts.TxDataNonZeroMultiplier;
        ulong accessListFloorCost = accessListFloorTokens * SilaAmsterdam.Instance.GasCosts.TotalCostFloorPerToken;
        ulong expectedRegular = GasCostOf.TransactionSip2780 + recipientRegular + accessListBaseCost + accessListFloorCost;
        ulong expectedFloorGas = GasCostOf.TransactionSip2780 + accessListFloorCost;

        Assert.That(splitIntrinsicGas.Standard.Value, Is.EqualTo(expectedRegular));
        Assert.That(splitIntrinsicGas.Standard.StateReservoir, Is.Zero);
        Assert.That(splitIntrinsicGas.FloorGas.Value, Is.EqualTo(expectedFloorGas));
        Assert.That(intrinsicGas.Standard, Is.EqualTo(expectedRegular));
        Assert.That(intrinsicGas.FloorGas, Is.EqualTo(expectedFloorGas));
    }

    [Test]
    public void Prague_access_list_floor_pricing_is_not_applied()
    {
        AccessList accessList = new AccessList.Builder()
            .AddAddress(TestItem.AddressA)
            .AddStorage(UInt256.One)
            .Build();
        Transaction tx = Build.A.Transaction.SignedAndResolved()
            .WithAccessList(accessList)
            .TestObject;

        IntrinsicGas<SilaGasPolicy> intrinsicGas = SilaGasPolicy.CalculateIntrinsicGas(tx, SilaPrague.Instance);
        ulong expectedRegular = GasCostOf.Transaction + GasCostOf.AccessAccountListEntry + GasCostOf.AccessStorageListEntry;

        Assert.That(intrinsicGas.Standard.Value, Is.EqualTo(expectedRegular));
        Assert.That(intrinsicGas.FloorGas.Value, Is.EqualTo(GasCostOf.Transaction));
    }

    [Test]
    public void State_gas_consumption_spills_to_regular_gas()
    {
        SilaGasPolicy gas = new() { Value = 100, StateReservoir = 50, StateGasUsed = 0 };

        bool consumed = SilaGasPolicy.ConsumeStateGas(ref gas, 70);

        Assert.That((consumed, gas.Value, gas.StateReservoir, gas.StateGasUsed), Is.EqualTo((true, 80L, 0L, 70L)));
    }

    [Test]
    public void ConsumeStateGas_oog_does_not_zero_reservoir()
    {
        SilaGasPolicy gas = new() { Value = 10, StateReservoir = 50, StateGasUsed = 0, StateGasSpill = 0 };

        bool consumed = SilaGasPolicy.ConsumeStateGas(ref gas, 70);

        Assert.That((consumed, gas.Value, gas.StateReservoir, gas.StateGasUsed, gas.StateGasSpill, SilaGasPolicy.IsOutOfGas(in gas)),
            Is.EqualTo((false, 10UL, 50L, 0L, 0L, true)));
    }

    [Test]
    public void Revoking_advanced_refund_keeps_refilled_gas_and_marked_spill()
    {
        // A net-spill (negative) reservoir, as left by RestoreChildStateGas after nested spills.
        SilaGasPolicy gas = new() { Value = 400, StateReservoir = -300, StateGasUsed = 0, StateGasSpill = 300, StateGasSpillRefunded = 0 };

        SilaGasPolicy.AddStateGasRefundToReservoir(ref gas, 200, trackSpillRefund: true);
        // LIFO refill: the whole advance (200 <= unrefunded spill 300) lands in gas_left and is marked.
        Assert.That((gas.Value, gas.StateReservoir, gas.StateGasSpillRefunded), Is.EqualTo((600UL, -300L, 200L)));

        SilaGasPolicy.RemoveStateGasRefundFromReservoir(ref gas, 200);

        // The claw-back drives the reservoir further negative; the refilled gas_left and the
        // permanent spill-refund mark stay in place.
        Assert.That((gas.Value, gas.StateReservoir, gas.StateGasUsed, gas.StateGasSpillRefunded), Is.EqualTo((600UL, -500L, 0L, 200L)));
    }

    [Test]
    public void Revoking_consumed_advanced_refund_deducts_usage_without_fabricating_spill_debt()
    {
        SilaGasPolicy gas = new() { Value = 400, StateReservoir = 0, StateGasUsed = 100 };

        SilaGasPolicy.AddStateGasRefundToReservoir(ref gas, 200, trackSpillRefund: true);
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref gas, 200), Is.True);

        SilaGasPolicy.RemoveStateGasRefundFromReservoir(ref gas, 200);

        Assert.That((gas.StateReservoir, gas.StateGasUsed), Is.EqualTo((0L, 100L)));
    }

    [Test]
    public void Intrinsic_regular_gas_above_the_tx_cap_saturates_into_the_reservoir()
    {
        SilaGasPolicy intrinsic = new() { Value = Sip7825Constants.DefaultTxGasLimitCap + 1, StateReservoir = 0 };

        SilaGasPolicy gas = SilaGasPolicy.CreateAvailableFromIntrinsic(
            Sip7825Constants.DefaultTxGasLimitCap + 2, in intrinsic, SilaAmsterdam.Instance);

        Assert.That((gas.Value, gas.StateReservoir), Is.EqualTo((0UL, 1L)));
    }

    [Test]
    public void Child_frame_gets_full_state_reservoir()
    {
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 333, StateGasUsed = 50 };

        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 444);

        Assert.That((parent.Value, parent.StateReservoir, parent.StateGasUsed, child.Value, child.StateReservoir, child.StateGasUsed),
            Is.EqualTo((1_000L, 0L, 50L, 444L, 333L, 0L)));
    }

    [Test]
    public void Child_frame_refund_restores_remaining_state_reservoir()
    {
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 333, StateGasUsed = 50 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 444);
        SilaGasPolicy.ConsumeStateGas(ref child, 100);
        SilaGasPolicy.UpdateGas(ref child, 150);

        SilaGasPolicy.Refund(ref parent, in child);

        Assert.That((parent.Value, parent.StateReservoir, parent.StateGasUsed), Is.EqualTo((1_294L, 233L, 150L)));
    }

    [Test]
    public void State_refund_is_clamped_to_intrinsic_state_floor()
    {
        SilaGasPolicy gas = new() { Value = 100, StateReservoir = 0, StateGasUsed = 120 };

        SilaGasPolicy.RefundStateGas(ref gas, 200, stateGasFloor: 40);

        Assert.That((gas.StateReservoir, gas.StateGasUsed), Is.EqualTo((80L, 40L)));
    }

    [Test]
    public void Code_insert_refund_credits_regular_gas_not_state_under_sip8038()
    {
        // The existing-authority refund returns the worst-case ACCOUNT_WRITE to the regular refund
        // counter and leaves the state dimension untouched (state refunds apply pre-execution).
        SilaGasPolicy gas = new()
        {
            Value = 0,
            StateReservoir = 0,
            StateGasUsed = GasCostOf.PerAuthBaseState,
        };

        ulong regularRefund = SilaGasPolicy.ApplyCodeInsertRefunds(ref gas, 1, SilaAmsterdam.Instance, stateGasFloor: 0);

        Assert.That(regularRefund, Is.EqualTo(Sip8038Constants.AccountWrite));
        Assert.That((gas.Value, gas.StateReservoir, gas.StateGasUsed, gas.StateGasSpill),
            Is.EqualTo((0L, 0L, GasCostOf.PerAuthBaseState, 0L)));
    }

    [Test]
    public void State_gas_refund_of_spilled_charge_returns_to_regular_gas_not_reservoir()
    {
        // Source-based (LIFO) refund: a charge that spilled into gas_left must be refunded to gas_left;
        // inflating the reservoir would let later operations draw state gas the spec says is unavailable.
        SilaGasPolicy gas = new() { Value = 10_000, StateReservoir = 0 };

        Assert.That(SilaGasPolicy.ConsumeStateGas(ref gas, 4000), Is.True);
        Assert.That((gas.Value, gas.StateReservoir, gas.StateGasUsed, gas.StateGasSpill),
            Is.EqualTo((6000L, 0L, 4000L, 4000L)), "charge with empty reservoir spills into regular gas");

        SilaGasPolicy.RefundStateGas(ref gas, 4000, stateGasFloor: 0);
        Assert.That((gas.Value, gas.StateReservoir, gas.StateGasUsed),
            Is.EqualTo((10_000L, 0L, 0L)), "spilled refund returns to regular gas, reservoir stays empty");
    }

    [Test]
    public void State_gas_refund_of_reservoir_charge_returns_to_reservoir()
    {
        // Complementary case: a reservoir-funded charge refunds back to the reservoir (no spill).
        SilaGasPolicy gas = new() { Value = 10_000, StateReservoir = 5000 };

        Assert.That(SilaGasPolicy.ConsumeStateGas(ref gas, 4000), Is.True);
        Assert.That((gas.Value, gas.StateReservoir, gas.StateGasUsed, gas.StateGasSpill),
            Is.EqualTo((10_000L, 1000L, 4000L, 0L)), "reservoir-funded charge does not touch regular gas");

        SilaGasPolicy.RefundStateGas(ref gas, 4000, stateGasFloor: 0);
        Assert.That((gas.Value, gas.StateReservoir, gas.StateGasUsed),
            Is.EqualTo((10_000L, 5000L, 0L)), "reservoir-funded refund returns to the reservoir");
    }

    [Test]
    public void Call_depth_exceeded_create_does_not_credit_state_gas_refund()
    {
        byte[] code = Prepare.SavmCode.Create([], UInt256.Zero).Done;
        CodeInfo codeInfo = CodeInfoFactory.CreateCodeInfo(code);
        ExecutionEnvironment env = ExecutionEnvironment.Rent(
            codeInfo,
            executingAccount: Recipient,
            caller: Sender,
            codeSource: Recipient,
            callDepth: VirtualMachineStatics.MaxCallDepth,
            value: UInt256.Zero,
            inputData: ReadOnlyMemory<byte>.Empty);
        SilaGasPolicy gas = new()
        {
            Value = 1_000_000,
            StateReservoir = GasCostOf.CreateState,
        };
        StackAccessTracker accessTracker = new();
        using VmState<SilaGasPolicy> vmState = VmState<SilaGasPolicy>.RentTopLevel(
            gas,
            ExecutionType.CALL,
            env,
            in accessTracker,
            Snapshot.Empty);

        Machine.SetBlockExecutionContext(new BlockExecutionContext(Build.A.Block.TestObject.Header, SilaAmsterdam.Instance));
        Machine.SetTxExecutionContext(new TxExecutionContext(Sender, CodeInfoRepository, null, UInt256.Zero));

        Machine.ExecuteTransaction<OffFlag>(vmState, TestState, NullTxTracer.Instance);

        Assert.That((vmState.Gas.StateGasUsed, vmState.Gas.StateReservoir, vmState.StateGasRefundAdvanced),
            Is.EqualTo((0L, GasCostOf.CreateState, 0L)));
    }

    [Test]
    public void Exceptional_halt_preserves_state_gas()
    {
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 500, StateGasUsed = 10 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 600);
        SilaGasPolicy.ConsumeStateGas(ref child, 200);

        SilaGasPolicy.SetOutOfGas(ref child);
        Assert.That((child.Value, child.StateReservoir), Is.EqualTo((0L, 300L)));

        SilaGasPolicy.RestoreChildStateGasOnHalt(ref parent, in child);
        Assert.That((parent.StateReservoir, parent.StateGasUsed), Is.EqualTo((500L, 10L)));
    }

    [Test]
    public void Exceptional_halt_does_not_inherit_child_spill()
    {
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 0, StateGasUsed = 0 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 1_000);
        SilaGasPolicy.ConsumeStateGas(ref child, 200);
        SilaGasPolicy.RefundStateGas(ref child, 200, stateGasFloor: 0);

        SilaGasPolicy.RestoreChildStateGasOnHalt(ref parent, in child);

        // The halt burns the child's refilled spill; its spill counters are not inherited.
        Assert.That((parent.StateReservoir, parent.StateGasUsed, parent.StateGasSpill, parent.StateGasSpillRefunded),
            Is.EqualTo((0L, 0L, 0L, 0L)));
    }

    [Test]
    public void Revert_returns_child_spill_to_parent_gas_left()
    {
        // A reverted child's net spill refills the parent gas_left; its spill counter
        // is not propagated.
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 0, StateGasUsed = 0 };
        SilaGasPolicy child = new()
        {
            Value = 100,
            StateReservoir = 0,
            StateGasUsed = 0,
            StateGasSpill = GasCostOf.CreateState + 3 * GasCostOf.SSetState,
        };

        SilaGasPolicy.RestoreChildStateGas(ref parent, in child);

        long netSpill = GasCostOf.CreateState + 3 * GasCostOf.SSetState;
        Assert.That((parent.Value, parent.StateReservoir), Is.EqualTo(((ulong)(1_000 + netSpill), -netSpill)));
        Assert.That(parent.StateGasSpill, Is.EqualTo(0L), "reverted child spill counter is not propagated");
    }

    [Test]
    public void Revert_with_fully_refunded_child_spill_returns_nothing_to_parent_gas_left()
    {
        // Net spill 0 after a full self-refund: revert adds nothing to the parent gas_left.
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 0, StateGasUsed = 0 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, GasCostOf.CreateState);
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref child, GasCostOf.CreateState), Is.True);
        SilaGasPolicy.RefundStateGas(ref child, GasCostOf.CreateState, stateGasFloor: 0);

        SilaGasPolicy.RestoreChildStateGas(ref parent, in child);

        Assert.That((parent.Value, parent.StateReservoir, parent.StateGasSpill), Is.EqualTo((1_000UL, 0L, 0L)),
            "net-0 reverted child adds nothing to parent gas_left and propagates no spill");
    }

    [Test]
    public void Refund_state_gas_marks_spilled_refund()
    {
        SilaGasPolicy gas = new() { Value = 1_000, StateReservoir = 0, StateGasUsed = 0 };
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref gas, 200), Is.True);

        SilaGasPolicy.RefundStateGas(ref gas, 80, stateGasFloor: 0);

        // LIFO: the refund returns to gas_left, not the reservoir, tracked in StateGasSpillRefunded.
        Assert.That((gas.StateGasUsed, gas.StateReservoir, gas.StateGasSpill, gas.StateGasSpillRefunded),
            Is.EqualTo((120L, 0L, 200L, 80L)));
        Assert.That(gas.Value, Is.EqualTo(880L));
    }

    [Test]
    public void Refund_state_gas_from_child_halt_preserves_spill_accounting()
    {
        SilaGasPolicy gas = new() { Value = GasCostOf.CreateState, StateReservoir = 0, StateGasUsed = 0 };
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref gas, GasCostOf.CreateState), Is.True);

        SilaGasPolicy.RefundStateGas(ref gas, GasCostOf.CreateState, stateGasFloor: 0, trackSpillRefund: false);

        Assert.That((gas.StateGasUsed, gas.StateReservoir, gas.StateGasSpill, gas.StateGasSpillRefunded),
            Is.EqualTo((0L, GasCostOf.CreateState, GasCostOf.CreateState, 0L)));
    }

    [Test]
    public void Refunded_spill_propagates_through_success_chain()
    {
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 0, StateGasUsed = 0 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 500);
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref child, 200), Is.True);
        SilaGasPolicy.RefundStateGas(ref child, 80, stateGasFloor: 0);

        SilaGasPolicy.Refund(ref parent, in child);

        Assert.That((parent.StateGasSpill, parent.StateGasSpillRefunded), Is.EqualTo((200L, 80L)));
    }

    [Test]
    public void Revert_does_not_inherit_partially_refunded_child_spill()
    {
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 0, StateGasUsed = 0 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 500);
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref child, 200), Is.True);
        SilaGasPolicy.RefundStateGas(ref child, 80, stateGasFloor: 0);

        SilaGasPolicy.RestoreChildStateGas(ref parent, in child);

        // The child's net spill (200 - 80 = 120) refills the parent gas_left; the parent's
        // own spill counters are not bumped.
        Assert.That((parent.Value, parent.StateReservoir, parent.StateGasUsed), Is.EqualTo((1_120UL, 0L, 0L)));
        Assert.That((parent.StateGasSpill, parent.StateGasSpillRefunded), Is.EqualTo((0L, 0L)));
    }

    [Test]
    public void Code_deposit_halt_keeps_refunded_child_spill_in_gas_left()
    {
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 0, StateGasUsed = 0 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 1_000);
        SilaGasPolicy.ConsumeStateGas(ref child, 200);
        SilaGasPolicy.RefundStateGas(ref child, 200, stateGasFloor: 0);

        SilaGasPolicy.Refund(ref parent, in child);
        SilaGasPolicy.RevertRefundToHalt(ref parent, in child);

        // The child spill was fully refunded (net 0), so nothing returns to the reservoir.
        Assert.That((parent.StateReservoir, parent.StateGasUsed, parent.StateGasSpill), Is.EqualTo((0L, 0L, 200L)));
        Assert.That(parent.StateGasSpillRefunded, Is.EqualTo(200L));
    }

    [Test]
    public void Code_deposit_halt_removes_merged_child_state_usage_without_refunding_reservoir_twice()
    {
        ulong parentRegularGas = 1_000;
        ulong childRegularGas = 500;
        long parentStateGasUsed = GasCostOf.CreateState;
        long childStateGasUsed = GasCostOf.NewAccountState + GasCostOf.SSetState;
        long childRemainingStateReservoir = 123;
        SilaGasPolicy parent = new()
        {
            Value = parentRegularGas,
            StateReservoir = parentStateGasUsed + childStateGasUsed + childRemainingStateReservoir,
            StateGasSpill = 77,
            StateGasSpillRefunded = 33,
        };

        Assert.That(SilaGasPolicy.ConsumeStateGas(ref parent, parentStateGasUsed), Is.True);
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, childRegularGas);
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref child, childStateGasUsed), Is.True);
        SilaGasPolicy.Refund(ref parent, in child);

        SilaGasPolicy.RevertRefundToHalt(ref parent, in child);

        Assert.That(
            (
                parent.Value,
                parent.StateReservoir,
                parent.StateGasUsed,
                parent.StateGasSpill,
                parent.StateGasSpillRefunded
            ),
            Is.EqualTo(
                (
                    parentRegularGas + childRegularGas,
                    childRemainingStateReservoir + childStateGasUsed,
                    parentStateGasUsed,
                    77L,
                    33L
                )));
    }

    [Test]
    public void ResetForHalt_snaps_state_gas_to_tx_start_shape()
    {
        // Architectural invariant: ResetForHalt resets the policy struct to its tx-start
        // shape — reservoir back to R0, StateGasUsed to the intrinsic floor, spill to 0.
        // The post-reset values feed SpentGas (= txGasLimit - remaining - reservoir) so
        // the user does not keep paying for state-gas they didn't get to commit.
        SilaGasPolicy gas = new() { Value = 100_000, StateReservoir = 1_000, StateGasUsed = 0 };

        Assert.That(SilaGasPolicy.ConsumeStateGas(ref gas, 5_000), Is.True);
        Assert.That((gas.StateReservoir, gas.StateGasUsed, gas.StateGasSpill), Is.EqualTo((0L, 5_000L, 4_000L)),
            "consume 5_000 state-gas with 1_000 reservoir => reservoir=0, used=5_000, spill=4_000");

        SilaGasPolicy.ResetForHalt(ref gas, initialStateReservoir: 1_000, initialStateGasUsed: 0);

        Assert.That(gas.StateReservoir, Is.EqualTo(1_000L), "reservoir snaps to R0");
        Assert.That(gas.StateGasUsed, Is.EqualTo(0L), "state-gas-used snaps to intrinsic floor");
        Assert.That(gas.StateGasSpill, Is.EqualTo(0L), "spill is zeroed");
    }

    [Test]
    public void Top_level_halt_block_state_gas_is_intrinsic_floor_not_spill()
    {
        // Top-level halt: block_state = post-reset StateGasUsed (intrinsic floor); the spilled
        // portion was paid from gas_left and burns as regular gas.
        SilaGasPolicy gas = new() { Value = 100_000, StateReservoir = 1_000, StateGasUsed = 0 };
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref gas, 5_000), Is.True);
        Assert.That((gas.StateReservoir, gas.StateGasUsed, gas.StateGasSpill), Is.EqualTo((0L, 5_000L, 4_000L)),
            "after consuming 5_000 with 1_000 reservoir: reservoir=0, used=5_000 (= 1_000 reservoir-portion + 4_000 spill), spill=4_000");

        SilaGasPolicy.ResetForHalt(ref gas, initialStateReservoir: 1_000, initialStateGasUsed: 0);

        // Block_state = post-reset StateGasUsed (0); the spill burns as regular gas.
        long blockLevelContribution = gas.StateGasUsed;
        Assert.That(blockLevelContribution, Is.EqualTo(0L),
            "block-level sum_state contribution = post-reset floor (0); spill is burned as regular gas");
        Assert.That((gas.StateReservoir, gas.StateGasSpill), Is.EqualTo((1_000L, 0L)));
    }

    [Test]
    public void Top_level_halt_block_state_gas_uses_post_reset_state_gas_used()
    {
        // Regression-guard: block_state must read the POST-reset StateGasUsed (intrinsic floor), not
        // the pre-reset value, or it over-counts by the refunded reservoir-portion.
        const long reservoirAtTxStart = 100_000;
        const long stateGasCharged = GasCostOf.SSetState;

        SilaGasPolicy gas = new() { Value = 1_000_000, StateReservoir = reservoirAtTxStart, StateGasUsed = 0 };
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref gas, stateGasCharged), Is.True);
        Assert.That(gas.StateGasSpill, Is.EqualTo(0L), "reservoir covers full charge; no spill");
        Assert.That(gas.StateGasUsed, Is.EqualTo(stateGasCharged), "full charge recorded in StateGasUsed");

        long preResetStateGasUsed = gas.StateGasUsed;
        SilaGasPolicy.ResetForHalt(ref gas, initialStateReservoir: reservoirAtTxStart, initialStateGasUsed: 0);

        Assert.That(preResetStateGasUsed - gas.StateGasUsed, Is.EqualTo(GasCostOf.SSetState),
            "reading pre-reset StateGasUsed instead of the post-reset floor ovsrcounts block-level sum_state by exactly the reservoir-portion (= 1 SSetState in this case)");
    }

    [Test]
    public void Top_level_halt_block_state_gas_per_tx_pattern_with_spill()
    {
        // Per-tx pattern with spill: the reservoir-portion is refunded and the spill is burned as
        // regular gas. Block-level sum_state contribution per halt = post-reset StateGasUsed
        // (intrinsic floor); the spill is NOT added to block_state.
        const long perTxGasLimit = 1_000_000;
        const long intrinsicStateGas = 0;
        const long reservoirAtTxStart = 100_000;
        const long stateGasCharged = 104_174;   // reservoir(100k) consumed + 4_174 spill

        SilaGasPolicy gas = new() { Value = perTxGasLimit, StateReservoir = reservoirAtTxStart, StateGasUsed = intrinsicStateGas };
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref gas, stateGasCharged), Is.True);
        Assert.That(gas.StateGasSpill, Is.EqualTo(4_174L), "4_174 spill from reservoir overflow");

        SilaGasPolicy.ResetForHalt(ref gas, initialStateReservoir: reservoirAtTxStart, initialStateGasUsed: intrinsicStateGas);

        long blockLevelContribution = gas.StateGasUsed;
        Assert.That(blockLevelContribution, Is.EqualTo(intrinsicStateGas),
            "per-tx block-level contribution = intrinsic floor; the spill is burned as regular gas");
    }

    [Test]
    public void Inner_revert_spill_refills_gas_left_and_is_not_propagated()
    {
        // SIP-8037: when a child frame REVERTS after spilling state gas from gas_left, the spill
        // refills the parent gas_left (source-based LIFO) and the child's spill counter is NOT
        // propagated — a reverted child never inflates the parent's unrefunded spill.
        SilaGasPolicy parent = new() { Value = 100_000, StateReservoir = 0, StateGasUsed = 0 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 50_000);
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref child, 4_174L), Is.True,
            "child consumes 4_174 state-gas with reservoir=0; entirely spills from gas_left");
        Assert.That(child.StateGasSpill, Is.EqualTo(4_174L));

        SilaGasPolicy.RestoreChildStateGas(ref parent, in child);

        Assert.That((parent.Value, parent.StateReservoir, parent.StateGasUsed), Is.EqualTo((104_174UL, 0L, 0L)),
            "child spill refills the parent gas_left; reservoir/used net to zero");
        Assert.That(parent.StateGasSpill, Is.EqualTo(0L), "reverted child spill is not propagated to the parent");
    }

    [Test]
    public void Reverted_grandchild_spill_does_not_propagate_through_success_chain()
    {
        // When a grandchild REVERTS with spill but its parent (child) succeeds, the reverted spill
        // is NOT propagated: RestoreChildStateGas refills gas_left without bumping the child's spill
        // counter, so the successful Refund carries no spill up to the top frame.
        SilaGasPolicy parent = new() { Value = 500_000, StateReservoir = 0, StateGasUsed = 0 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 400_000);

        SilaGasPolicy grandchild = SilaGasPolicy.CreateChildFrameGas(ref child, 200_000);
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref grandchild, 4_174L), Is.True);
        SilaGasPolicy.RestoreChildStateGas(ref child, in grandchild);
        Assert.That(child.StateGasSpill, Is.EqualTo(0L), "reverted grandchild spill is not inherited by the child");

        SilaGasPolicy.Refund(ref parent, in child);

        Assert.That(parent.StateGasSpill, Is.EqualTo(0L),
            "no spill propagates to the top frame through the success path");
    }

    [Test]
    public void Reset_for_halt_preserves_gas_left_and_spill_refund_mark()
    {
        // ResetForHalt snaps only the three state-shape fields (reservoir/used/spill); gas_left and
        // the tx-wide StateGasSpillRefunded mark are left untouched.
        SilaGasPolicy gas = new()
        {
            Value = 4_242,
            StateReservoir = 0,
            StateGasUsed = GasCostOf.CreateState,
            StateGasSpill = GasCostOf.CreateState,
            StateGasSpillRefunded = 4_174,
        };

        SilaGasPolicy.ResetForHalt(ref gas, initialStateReservoir: 0, initialStateGasUsed: GasCostOf.CreateState);

        Assert.That((gas.Value, gas.StateGasSpillRefunded), Is.EqualTo((4_242UL, 4_174L)),
            "ResetForHalt preserves gas_left and the StateGasSpillRefunded mark");
        Assert.That(gas.StateGasSpill, Is.EqualTo(0L), "spill is zeroed by the reset");
    }

    [Test]
    public void Top_level_halt_block_state_is_full_intrinsic_not_reduced_by_spill()
    {
        // New halt model (RefundOnTopLevelHalt): block_state = full intrinsic floor. Unlike the OLD
        // model that subtracted burned spill from block_state (moving it to block_regular), the spill
        // is now burned as regular gas via gas_left, so no explicit block_state subtraction is applied.
        const long intrinsicStateGas = GasCostOf.CreateState;
        const long innerRevertSpill = 4_174;

        long newBlockState = intrinsicStateGas;
        long oldBlockState = Math.Max(0, intrinsicStateGas - innerRevertSpill);

        Assert.That(newBlockState - oldBlockState, Is.EqualTo(innerRevertSpill),
            "the new model no longer subtracts burned spill from block_state; it stays at the full intrinsic floor");
    }

    [Test]
    public void Revert_restores_state_gas_to_parent_reservoir()
    {
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 400, StateGasUsed = 20 };
        SilaGasPolicy.Consume(ref parent, 600);
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 600);
        SilaGasPolicy.UpdateGas(ref child, 100);
        SilaGasPolicy.ConsumeStateGas(ref child, 150);

        SilaGasPolicy.UpdateGasUp(ref parent, SilaGasPolicy.GetRemainingGas(in child));
        SilaGasPolicy.RestoreChildStateGas(ref parent, in child);

        Assert.That((parent.Value, parent.StateReservoir, parent.StateGasUsed), Is.EqualTo((900L, 400L, 20L)));
    }

    [Test]
    public void Revert_restores_child_inline_state_refund_to_parent_reservoir()
    {
        SilaGasPolicy parent = new() { Value = 1_000, StateReservoir = 400, StateGasUsed = 20 };
        SilaGasPolicy child = SilaGasPolicy.CreateChildFrameGas(ref parent, 600);
        SilaGasPolicy.ConsumeStateGas(ref child, 150);
        SilaGasPolicy.RefundStateGas(ref child, 40, stateGasFloor: 0);

        SilaGasPolicy.RestoreChildStateGas(ref parent, in child);

        Assert.That((parent.StateReservoir, parent.StateGasUsed), Is.EqualTo((400L, 20L)));
    }

    [Test]
    public void Revert_does_not_propagate_fully_refunded_descendant_spill()
    {
        SilaGasPolicy parent = new() { Value = 500_000, StateReservoir = 0, StateGasUsed = 0 };
        SilaGasPolicy outer = SilaGasPolicy.CreateChildFrameGas(ref parent, 400_000);
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref outer, GasCostOf.CreateState), Is.True);

        SilaGasPolicy inner = SilaGasPolicy.CreateChildFrameGas(ref outer, 200_000);
        Assert.That(SilaGasPolicy.ConsumeStateGas(ref inner, GasCostOf.SSetState), Is.True);
        SilaGasPolicy.RefundStateGas(ref inner, GasCostOf.SSetState, stateGasFloor: 0);
        SilaGasPolicy.RestoreChildStateGas(ref outer, in inner);

        SilaGasPolicy.RefundStateGas(ref outer, GasCostOf.CreateState, stateGasFloor: 0);
        SilaGasPolicy.RestoreChildStateGas(ref parent, in outer);

        // Both refunds returned to gas_left; the outer frame reverts, so no spill (nor its refund
        // mark) is propagated up to the ancestor, and gas_left returns to its starting value.
        Assert.That((parent.Value, parent.StateReservoir, parent.StateGasUsed, parent.StateGasSpill),
            Is.EqualTo((500_000UL, 0L, 0L, 0L)));
        Assert.That(parent.StateGasSpillRefunded, Is.EqualTo(0L),
            "reverted descendant spill and its refund mark are not inherited by the ancestor");
    }

    [TestCase(ExpectedResult = 5_000L)]
    public long Spent_gas_subtracts_state_reservoir()
    {
        SilaGasPolicy gas = new() { Value = 3_000, StateReservoir = 2_000, StateGasUsed = 500 };
        return 10_000L - (long)SilaGasPolicy.GetRemainingGas(in gas) - SilaGasPolicy.GetStateReservoir(in gas);
    }
}
