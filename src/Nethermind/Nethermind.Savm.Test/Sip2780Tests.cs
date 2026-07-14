// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Threading.Tasks;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Core.Test.Blockchain;
using Nethermind.Core.Test.Builders;
using Nethermind.Savm.GasPolicy;
using Nethermind.Specs;
using Nethermind.Specs.Forks;
using Nethermind.Specs.Test;
using Nethermind.State;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

/// <summary>Pins the SIP-2780 gas-policy primitives and exercises the intrinsic path end-to-end.</summary>
[TestFixture]
public class Sip2780Tests
{
    private static readonly IReleaseSpec Sip2780Spec = new OverridableReleaseSpec(SilaPrague.Instance) { IsSip2780Enabled = true };

    [Test]
    public void Call_value_cost_is_flat_under_sip2780()
    {
        SilaGasPolicy gas = SilaGasPolicy.FromULong(1_000_000);
        Assert.That(SilaGasPolicy.ConsumeCallValueTransferSip2780(ref gas), Is.True);
        Assert.That(1_000_000 - SilaGasPolicy.GetRemainingGas(in gas), Is.EqualTo(Sip8038Constants.CallValue));
    }

    private static ulong ChargeAccountAccess(IReleaseSpec spec, bool prewarm)
    {
        SilaGasPolicy gas = SilaGasPolicy.FromULong(1_000_000);
        using StackAccessTracker tracker = new();
        if (prewarm) tracker.WarmUp(TestItem.AddressB);
        Assert.That(SilaGasPolicy.ConsumeAccountAccessGas(ref gas, spec, in tracker, isTracingAccess: false, TestItem.AddressB), Is.True);
        return 1_000_000 - SilaGasPolicy.GetRemainingGas(in gas);
    }

    [Test]
    public void Cold_account_touch_is_flat()
    {
        Assert.That(ChargeAccountAccess(Sip2780Spec, prewarm: false), Is.EqualTo(GasCostOf.ColdAccountAccess), "cold account");
        Assert.That(ChargeAccountAccess(Sip2780Spec, prewarm: true), Is.EqualTo(GasCostOf.WarmStateRead), "warm account stays at WARM_STATE_READ");
    }

    private static Task<BasicTestBlockchain> CreateChain() =>
        BasicTestBlockchain.Create(b => b.AddSingleton<ISpecProvider>(
            new TestSpecProvider(new OverridableReleaseSpec(SilaPrague.Instance) { IsSip2780Enabled = true, IsSip7708Enabled = true })));

    // Whole-transaction totals; recipient existence is irrelevant (state-independent intrinsic).
    [TestCase(false, 1ul, GasCostOf.TransactionSip2780 + Sip8038Constants.ColdAccountAccess + GasCostOf.TxValueCostSip2780 + GasCostOf.TransferLogSip2780, TestName = "value transfer to existing EOA (21000)")]
    [TestCase(true, 1ul, GasCostOf.TransactionSip2780 + Sip8038Constants.ColdAccountAccess + GasCostOf.TxValueCostSip2780 + GasCostOf.TransferLogSip2780, TestName = "value transfer to new account (21000)")]
    [TestCase(false, 0ul, GasCostOf.TransactionSip2780 + Sip8038Constants.ColdAccountAccess, TestName = "no-transfer to existing EOA (15000)")]
    [TestCase(true, 0ul, GasCostOf.TransactionSip2780 + Sip8038Constants.ColdAccountAccess, TestName = "no-transfer to empty account (15000)")]
    public async Task Simple_transfer_spends_sip2780_total_gas(bool recipientIsNew, ulong value, ulong expectedGas)
    {
        using BasicTestBlockchain chain = await CreateChain();
        ulong nonce = chain.StateReader.GetNonce(chain.BlockTree.Head!.Header, TestItem.AddressA);

        Address recipient = recipientIsNew ? TestItem.AddressF : TestItem.AddressB;
        Transaction tx = Build.A.Transaction
            .WithTo(recipient)
            .WithValue(value)
            .WithNonce(nonce)
            .WithGasLimit(60000)
            .SignedAndResolved(TestItem.PrivateKeyA)
            .TestObject;

        Block block = await chain.AddBlock(tx);

        Assert.That(chain.ReceiptStorage.Get(block)[0].GasUsed, Is.EqualTo(expectedGas));
    }
}
