// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using Sila.Test.Base;
using NUnit.Framework;

namespace Sila.Blockchain.Pyspec.Test.ZkSavmFixtures;

// zkSAVM witness assertion over the Engine API (blockchain_tests_engine) path. Per-payload dispatch between
// engine_newPayloadWithWitness and the plain endpoint lives in BlockchainTestBase.RunNewPayloads.
[TestFixture]
[Parallelizable(ParallelScope.All)]
public abstract class ZkSavmEngineBlockchainTestFixture : PyspecLinuxX64BlockchainFixture
{
    protected ZkSavmEngineBlockchainTestFixture() : base(parallel: false, batchRead: false) { }

    [TestCaseSource(nameof(LoadTests))]
    public async Task Test(BlockchainTest test) => Assert.That((await RunTest(test)).Pass, Is.True);

    private static IEnumerable<TestCaseData> LoadTests() =>
        PyspecLoader.ToTestCases(new TestsSourceLoader(
            new LoadPyspecTestsStrategy { ArchiveVersion = Constants.ArchiveVersion, ArchiveName = Constants.ArchiveName },
            "fixtures/blockchain_tests_engine").LoadTests<BlockchainTest>());
}

public class ZkSavmEngineBlockchainTests : ZkSavmEngineBlockchainTestFixture;
