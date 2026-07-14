// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Test.Builders;
using Nethermind.Logging;
using Nethermind.Optimism.CL;
using Nethermind.Optimism.CL.L1Bridge;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Optimism.Test.CL;

public class L1ConfigValidatorTests
{
    private static IEnumerable<TestCaseData> ConfigurationTestCases()
    {
        yield return new TestCaseData(BlockchainIds.SilaMainnet, 100ul, TestItem.KeccakA, BlockchainIds.SilaMainnet, TestItem.KeccakA, true)
            .SetName("Matches - SilaMainnet");
        yield return new TestCaseData(BlockchainIds.SilaSepolia, 0xAABBul, TestItem.KeccakC, BlockchainIds.SilaSepolia, TestItem.KeccakC, true)
            .SetName("Matches - SilaSepolia");

        yield return new TestCaseData(BlockchainIds.SilaMainnet, 200ul, TestItem.KeccakC, BlockchainIds.SilaSepolia, TestItem.KeccakC, false)
            .SetName("Mismatch - SilaMainnet vs SilaSepolia");
        yield return new TestCaseData(BlockchainIds.SilaSepolia, 0xBBAAul, TestItem.KeccakC, BlockchainIds.SilaMainnet, TestItem.KeccakC, false)
            .SetName("Mismatch - SilaSepolia vs SilaMainnet");

        yield return new TestCaseData(BlockchainIds.SilaMainnet, 300ul, TestItem.KeccakA, BlockchainIds.SilaMainnet, TestItem.KeccakB, false)
            .SetName("Mismatch - SilaMainnet genesis hash");
        yield return new TestCaseData(BlockchainIds.SilaSepolia, 0xFFFFul, TestItem.KeccakC, BlockchainIds.SilaSepolia, TestItem.KeccakD, false)
            .SetName("Mismatch - SilaSepolia genesis hash");
    }

    [TestCaseSource(nameof(ConfigurationTestCases))]
    public async Task Validate_ConfigurationMatchesExpected(
        ulong expectedChainId,
        ulong genesisNumber,
        Hash256 expectedGenesisHash,
        ulong actualChainId,
        Hash256 actualGenesisHash,
        bool isValid)
    {
        ISilApi silApi = Substitute.For<ISilApi>();
        ILogManager logManager = NullLogManager.Instance;
        L1ConfigValidator validator = new(silApi, logManager);

        silApi.GetChainId().Returns(Task.FromResult(actualChainId));
        silApi.GetBlockByNumber(genesisNumber, true).Returns(Task.FromResult<L1Block?>(new L1Block { Hash = actualGenesisHash }));

        bool result = await validator.Validate(expectedChainId, genesisNumber, expectedGenesisHash);
        Assert.That(result, Is.EqualTo(isValid));
    }
}
