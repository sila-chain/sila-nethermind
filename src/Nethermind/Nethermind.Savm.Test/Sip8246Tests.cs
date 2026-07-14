// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Crypto;
using Nethermind.Savm.State;
using Nethermind.Savm.Tracing;
using Nethermind.Savm.TransactionProcessing;
using Nethermind.Int256;
using Nethermind.Specs;
using Nethermind.Specs.Forks;
using Nethermind.Specs.Test;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

/// <summary>
/// SIP-8246: Remove SELFDESTRUCT Burn — destroyed accounts keep their balance while code/storage
/// clear and the nonce resets; a resulting zero-balance account is still removed per SIP-161.
/// </summary>
/// <remarks>
/// Fixture args: SIP-8246 on/off, and SIP-8037+7708 on/off (the latter selects the deferred
/// SilaAmsterdam-style finalization path over the inline one).
/// </remarks>
[TestFixture(true, false)]
[TestFixture(false, false)]
[TestFixture(true, true)]
[TestFixture(false, true)]
public class Sip8246Tests(bool sip8246Enabled, bool deferredFinalization) : VirtualMachineTestsBase
{
    private readonly ISpecProvider _specProvider =
        new TestSpecProvider(new OverridableReleaseSpec(SilaCancun.Instance)
        {
            IsSip8246Enabled = sip8246Enabled,
            // SIP-8037 + SIP-7708 together select the deferred finalization path.
            IsSip8037Enabled = deferredFinalization,
            IsSip7708Enabled = deferredFinalization,
        });

    protected override ulong BlockNumber => MainnetSpecProvider.ParisBlockNumber;
    protected override ulong Timestamp => MainnetSpecProvider.CancunBlockTimestamp;
    protected override ISpecProvider SpecProvider => _specProvider;

    // Generous limit so the SIP-8037 state-byte charges in the deferred-path fixtures don't run out of gas.
    private const ulong GasLimit = 5_000_000;
    private static readonly byte[] Salt = new UInt256(123).ToBigEndian();

    private SilaEcdsa _ecdsa;
    // Runtime code that writes storage slot 0, then self-destructs to its own address
    // (SSTORE slot0=1; ADDRESS; SELFDESTRUCT). The SSTORE lets tests observe storage clearing.
    private byte[] _sstoreThenSelfDestructToSelf;
    private byte[] _sstoreThenSelfDestructToSelfInit;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _ecdsa = new SilaEcdsa(SpecProvider.ChainId);
        TestState.CreateAccount(TestItem.PrivateKeyA.Address, 1000.Sila);
        TestState.Commit(SpecProvider.GenesisSpec);
        TestState.CommitTree(0);

        _sstoreThenSelfDestructToSelf = Prepare.SavmCode
            .PushData(1)
            .PushData(0)
            .Op(Instruction.SSTORE)
            .Op(Instruction.ADDRESS)
            .Op(Instruction.SELFDESTRUCT)
            .Done;
        _sstoreThenSelfDestructToSelfInit = Prepare.SavmCode
            .ForInitOf(_sstoreThenSelfDestructToSelf)
            .Done;
    }

    [TestCase(99, TestName = "same-tx self-destruct to self, non-zero balance")]
    [TestCase(0, TestName = "same-tx self-destruct to self, zero balance")]
    public void Same_tx_self_destruct_to_self_does_not_burn(int balanceEther)
    {
        UInt256 balance = balanceEther.Sila;
        Address createTxAddress = ContractAddress.From(TestItem.PrivateKeyA.Address, 0);
        Address child = ContractAddress.From(createTxAddress, Salt, _sstoreThenSelfDestructToSelfInit);

        byte[] code = Prepare.SavmCode
            .Create2(_sstoreThenSelfDestructToSelfInit, Salt, balance)
            .Call(child, 500_000)
            .STOP()
            .Done;

        ExecuteTopLevel(code, value: 100.Sila);

        if (sip8246Enabled && !balance.IsZero)
        {
            // Balance preserved; account survives as a fresh balance-only account.
            AssertBalanceOnly(child, balance);
        }
        else
        {
            // Pre-8246 the self-burn empties the account; a zero-balance account is empty either way.
            Assert.That(TestState.AccountExists(child), Is.False);
        }
    }

    [Test]
    public void Same_tx_created_then_receives_eth_does_not_burn()
    {
        // Contract A self-destructs to an inheritor when called with zero value, otherwise just
        // accepts SIL. Contract B creates A, calls it (selfdestruct), then sends it more SIL.
        Address inheritor = TestItem.AddressE;
        byte[] contractACode = Prepare.SavmCode
            .CALLVALUE()
            .Op(Instruction.ISZERO)
            .PushData(6)
            .JUMPI()
            .STOP()
            .JUMPDEST()
            .SELFDESTRUCT(inheritor)
            .Done;
        byte[] initCodeA = Prepare.SavmCode.ForInitOf(contractACode).Done;

        UInt256 initialBalance = 3.Sila;
        UInt256 silReceivedAfter = 2.Sila;

        // The create-tx body itself acts as contract B: it creates A, calls it, then funds it.
        Address contractB = ContractAddress.From(TestItem.PrivateKeyA.Address, 0);
        Address contractA = ContractAddress.From(contractB, 1);

        byte[] contractBCode = Prepare.SavmCode
            .Create(initCodeA, initialBalance)
            .Call(contractA, 500_000)                                  // triggers selfdestruct-to-inheritor
            .CallWithValue(contractA, 500_000, silReceivedAfter)      // sends SIL after selfdestruct
            .STOP()
            .Done;

        ExecuteTopLevel(contractBCode, value: 10.Sila);

        // Initial balance always leaves via the inheritor (transfer, not burn).
        Assert.That(TestState.GetBalance(inheritor), Is.EqualTo(initialBalance));

        if (sip8246Enabled)
        {
            // The SIL received after SELFDESTRUCT is preserved rather than burned at finalization.
            AssertBalanceOnly(contractA, silReceivedAfter);
        }
        else
        {
            Assert.That(TestState.AccountExists(contractA), Is.False);
        }
    }

    [Test]
    public void Same_tx_self_destruct_to_other_still_transfers()
    {
        // SIP-8246 must not change the transfer path: balance goes to the inheritor and the
        // now-empty account is removed regardless of the flag.
        Address inheritor = TestItem.AddressE;
        byte[] runtime = Prepare.SavmCode.SELFDESTRUCT(inheritor).Done;
        byte[] init = Prepare.SavmCode.ForInitOf(runtime).Done;

        Address createTxAddress = ContractAddress.From(TestItem.PrivateKeyA.Address, 0);
        Address child = ContractAddress.From(createTxAddress, Salt, init);

        byte[] code = Prepare.SavmCode
            .Create2(init, Salt, 5.Sila)
            .Call(child, 500_000)
            .STOP()
            .Done;

        ExecuteTopLevel(code, value: 10.Sila);

        Assert.That(TestState.GetBalance(inheritor), Is.EqualTo((UInt256)5.Sila));
        Assert.That(TestState.AccountExists(child), Is.False);
    }

    [Test]
    public void Self_destruct_to_self_not_in_same_tx_is_unchanged_no_op()
    {
        // Already a no-op since SIP-6780; SIP-8246 leaves it untouched (balance kept, code intact).
        byte[] runtime = _sstoreThenSelfDestructToSelf;
        byte[] init = Prepare.SavmCode.ForInitOf(runtime).Done;
        Address contract = ContractAddress.From(TestItem.PrivateKeyA.Address, 0);

        Transaction deployTx = Build.A.Transaction.WithCode(init).WithValue(7.Sila)
            .WithGasLimit(GasLimit).SignedAndResolved(_ecdsa, TestItem.PrivateKeyA).TestObject;
        byte[] call = Prepare.SavmCode.Call(contract, 500_000).STOP().Done;
        Transaction callTx = Build.A.Transaction.WithCode(call).WithGasLimit(GasLimit)
            .WithNonce(1).SignedAndResolved(_ecdsa, TestItem.PrivateKeyA).TestObject;

        Block block = Build.A.Block.WithNumber(BlockNumber).WithTimestamp(Timestamp)
            .WithTransactions(deployTx, callTx).WithGasLimit(2 * GasLimit).TestObject;
        BlockExecutionContext blCtx = new(block.Header, SpecProvider.GetSpec(block.Header));

        _processor.Execute(deployTx, blCtx, NullTxTracer.Instance);
        _processor.Execute(callTx, blCtx, NullTxTracer.Instance);

        Assert.That(TestState.GetBalance(contract), Is.EqualTo((UInt256)7.Sila));
        Assert.That(TestState.IsContract(contract), Is.True);
    }

    [Test]
    public void Create2_redeploy_to_same_address_unblocked_after_self_destruct()
    {
        // The child self-destructs in its own creation tx; SIP-8246 preserves it as a nonce-0,
        // code-less account, so a second CREATE2 to the same address is not blocked.
        UInt256 endowment = 1.Sila;

        Address factory = ContractAddress.From(TestItem.PrivateKeyA.Address, 0);
        Address child = ContractAddress.From(factory, Salt, _sstoreThenSelfDestructToSelf);

        byte[] factoryRuntime = Prepare.SavmCode
            .Create2(_sstoreThenSelfDestructToSelf, Salt, endowment)
            .STOP()
            .Done;
        byte[] factoryInit = Prepare.SavmCode.ForInitOf(factoryRuntime).Done;

        Transaction deploy = Build.A.Transaction.WithCode(factoryInit).WithValue(5.Sila)
            .WithNonce(0).WithGasLimit(GasLimit).SignedAndResolved(_ecdsa, TestItem.PrivateKeyA).TestObject;
        Transaction call1 = Build.A.Transaction.WithTo(factory)
            .WithNonce(1).WithGasLimit(GasLimit).SignedAndResolved(_ecdsa, TestItem.PrivateKeyA).TestObject;
        Transaction call2 = Build.A.Transaction.WithTo(factory)
            .WithNonce(2).WithGasLimit(GasLimit).SignedAndResolved(_ecdsa, TestItem.PrivateKeyA).TestObject;

        Block block = Build.A.Block.WithNumber(BlockNumber).WithTimestamp(Timestamp)
            .WithTransactions(deploy, call1, call2).WithGasLimit(4 * GasLimit).TestObject;
        BlockExecutionContext blCtx = new(block.Header, SpecProvider.GetSpec(block.Header));

        _processor.Execute(deploy, blCtx, NullTxTracer.Instance);
        _processor.Execute(call1, blCtx, NullTxTracer.Instance);
        _processor.Execute(call2, blCtx, NullTxTracer.Instance);

        if (sip8246Enabled)
        {
            // Both redeployments funded the same preserved address: 2 x endowment.
            AssertBalanceOnly(child, 2.Sila);
        }
        else
        {
            // Pre-8246 the self-burn empties the child on each pass, so it ends deleted.
            Assert.That(TestState.AccountExists(child), Is.False);
        }
    }

    private void ExecuteTopLevel(byte[] code, UInt256 value)
    {
        Transaction tx = Build.A.Transaction.WithCode(code).WithValue(value)
            .WithGasLimit(GasLimit).SignedAndResolved(_ecdsa, TestItem.PrivateKeyA).TestObject;
        Block block = Build.A.Block.WithNumber(BlockNumber).WithTimestamp(Timestamp)
            .WithTransactions(tx).WithGasLimit(2 * GasLimit).TestObject;
        _processor.Execute(tx, new BlockExecutionContext(block.Header, SpecProvider.GetSpec(block.Header)), NullTxTracer.Instance);
    }

    private void AssertBalanceOnly(Address address, UInt256 expectedBalance)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(TestState.AccountExists(address), Is.True);
            Assert.That(TestState.GetBalance(address), Is.EqualTo(expectedBalance), "balance preserved");
            Assert.That(TestState.GetNonce(address), Is.EqualTo(0UL), "nonce reset");
            Assert.That(TestState.IsContract(address), Is.False, "code cleared");
            Assert.That(TestState.Get(new StorageCell(address, UInt256.Zero)).IsZero(), Is.True, "storage cleared");
        }
    }
}
