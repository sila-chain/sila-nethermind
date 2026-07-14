// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Blockchain.Blocks;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Sip2930;
using Nethermind.Core.Test;
using Nethermind.Core.Test.Builders;
using Nethermind.Savm.State;
using Nethermind.Specs;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Xdc.Test;

internal class XdcBlockhashStoreTests
{
    private static readonly Address Sip2935Account = Sip2935Constants.BlockHashHistoryAddress;

    private static ReleaseSpec Sip2935Spec => new()
    {
        IsSip2935Enabled = true,
        Sip2935RingBufferSize = Sip2935Constants.RingBufferSize,
    };

    // Dropping IHasAccessList from the decorator silently disables the inner store's prewarm hint.
    [TestCase(true, TestName = "GetAccessList_WithHintCapableInner_ForwardsTheInnerHint")]
    [TestCase(false, TestName = "GetAccessList_WithHintlessInner_IsNull")]
    public void GetAccessList_AtGivenInnerCapability_ForwardsExactly(bool innerHintCapable)
    {
        IWorldState worldState = TestWorldStateFactory.CreateForTest();
        using IDisposable _ = worldState.BeginScope(IWorldState.PreGenesis);
        ReleaseSpec spec = Sip2935Spec;
        Block block = Build.A.Block.WithNumber(42).WithParentHash(TestItem.KeccakA).TestObject;
        IBlockhashStore inner = innerHintCapable ? new BlockhashStore(worldState) : Substitute.For<IBlockhashStore>();
        XdcBlockhashStore store = new(inner, worldState);
        store.ApplyBlockhashStateChanges(block.Header, spec);

        AccessList? hint = store.GetAccessList(block, spec);

        if (!innerHintCapable)
        {
            Assert.That(hint, Is.Null);
            return;
        }

        Assert.That(hint, Is.Not.Null, "the decorator must forward the inner store's hint");
        foreach ((Address address, AccessList.StorageKeysEnumerable _) in hint!)
        {
            Assert.That(address, Is.EqualTo(Sip2935Account));
        }
    }

    [Test]
    public void Deploys_history_contract_when_missing()
    {
        IWorldState worldState = TestWorldStateFactory.CreateForTest();
        using IDisposable _ = worldState.BeginScope(IWorldState.PreGenesis);
        ReleaseSpec spec = Sip2935Spec;
        BlockHeader header = Build.A.BlockHeader.WithNumber(42).WithParentHash(TestItem.KeccakA).TestObject;

        XdcBlockhashStore store = new(new BlockhashStore(worldState), worldState);
        store.ApplyBlockhashStateChanges(header, spec);

        Assert.Multiple(() =>
        {
            Assert.That(worldState.IsContract(Sip2935Account), Is.True);
            Assert.That(worldState.GetCode(Sip2935Account), Is.EqualTo(Sip2935Constants.Code));
            Assert.That(worldState.GetNonce(Sip2935Account), Is.EqualTo(1UL));
        });
    }

    [Test]
    public void Stores_parent_hash_after_deploying_contract()
    {
        IWorldState worldState = TestWorldStateFactory.CreateForTest();
        using IDisposable _ = worldState.BeginScope(IWorldState.PreGenesis);
        ReleaseSpec spec = Sip2935Spec;
        BlockHeader header = Build.A.BlockHeader.WithNumber(42).WithParentHash(TestItem.KeccakA).TestObject;

        XdcBlockhashStore store = new(new BlockhashStore(worldState), worldState);
        store.ApplyBlockhashStateChanges(header, spec);

        Hash256? stored = store.GetBlockHashFromState(header, header.Number - 1, spec);
        Assert.That(stored, Is.EqualTo(header.ParentHash));
    }

    [Test]
    public void Does_not_redeploy_when_history_contract_already_present()
    {
        IWorldState worldState = TestWorldStateFactory.CreateForTest();
        using IDisposable _ = worldState.BeginScope(IWorldState.PreGenesis);
        ReleaseSpec spec = Sip2935Spec;

        const ulong existingNonce = 7;
        worldState.CreateAccount(Sip2935Account, 0, existingNonce);
        worldState.InsertCode(Sip2935Account, ValueKeccak.Compute(Sip2935Constants.Code), Sip2935Constants.Code, spec);
        worldState.Commit(spec);

        BlockHeader header = Build.A.BlockHeader.WithNumber(42).WithParentHash(TestItem.KeccakA).TestObject;
        XdcBlockhashStore store = new(new BlockhashStore(worldState), worldState);
        store.ApplyBlockhashStateChanges(header, spec);

        Assert.Multiple(() =>
        {
            // Nonce is left untouched — the contract is not redeployed.
            Assert.That(worldState.GetNonce(Sip2935Account), Is.EqualTo(existingNonce));
            Assert.That(store.GetBlockHashFromState(header, header.Number - 1, spec), Is.EqualTo(header.ParentHash));
        });
    }

    [Test]
    public void Throws_when_history_contract_code_mismatches()
    {
        IWorldState worldState = TestWorldStateFactory.CreateForTest();
        using IDisposable _ = worldState.BeginScope(IWorldState.PreGenesis);
        ReleaseSpec spec = Sip2935Spec;

        byte[] wrongCode = [1, 2, 3];
        worldState.CreateAccount(Sip2935Account, 0, 1);
        worldState.InsertCode(Sip2935Account, ValueKeccak.Compute(wrongCode), wrongCode, spec);
        worldState.Commit(spec);

        BlockHeader header = Build.A.BlockHeader.WithNumber(42).WithParentHash(TestItem.KeccakA).TestObject;
        XdcBlockhashStore store = new(new BlockhashStore(worldState), worldState);

        Assert.Throws<InvalidOperationException>(() => store.ApplyBlockhashStateChanges(header, spec));
    }

    [Test]
    public void Does_nothing_for_genesis_block()
    {
        IWorldState worldState = TestWorldStateFactory.CreateForTest();
        using IDisposable _ = worldState.BeginScope(IWorldState.PreGenesis);
        ReleaseSpec spec = Sip2935Spec;
        BlockHeader genesis = Build.A.BlockHeader.WithNumber(0).TestObject;

        XdcBlockhashStore store = new(new BlockhashStore(worldState), worldState);
        store.ApplyBlockhashStateChanges(genesis, spec);

        Assert.That(worldState.AccountExists(Sip2935Account), Is.False);
    }

    [Test]
    public void Does_nothing_when_sip2935_disabled()
    {
        IWorldState worldState = TestWorldStateFactory.CreateForTest();
        using IDisposable _ = worldState.BeginScope(IWorldState.PreGenesis);
        ReleaseSpec spec = new() { IsSip2935Enabled = false };
        BlockHeader header = Build.A.BlockHeader.WithNumber(42).WithParentHash(TestItem.KeccakA).TestObject;

        XdcBlockhashStore store = new(new BlockhashStore(worldState), worldState);
        store.ApplyBlockhashStateChanges(header, spec);

        Assert.That(worldState.AccountExists(Sip2935Account), Is.False);
    }

    [Test]
    public void Does_nothing_when_parent_hash_missing()
    {
        IWorldState worldState = TestWorldStateFactory.CreateForTest();
        using IDisposable _ = worldState.BeginScope(IWorldState.PreGenesis);
        ReleaseSpec spec = Sip2935Spec;
        BlockHeader header = Build.A.BlockHeader.WithNumber(42).TestObject;
        header.ParentHash = null;

        XdcBlockhashStore store = new(new BlockhashStore(worldState), worldState);
        store.ApplyBlockhashStateChanges(header, spec);

        Assert.That(worldState.AccountExists(Sip2935Account), Is.False);
    }
}
