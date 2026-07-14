// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Linq;
using Nethermind.Consensus.Validators;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Logging;
using Nethermind.Specs.Forks;
using Nethermind.Specs.Test;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Blockchain.Test.Validators;

[Parallelizable(ParallelScope.All)]
public class ShardBlobBlockValidatorTests
{
    [TestCaseSource(nameof(BlobGasFieldsPerForkTestCases))]
    public static bool Blob_gas_fields_should_be_set(IReleaseSpec spec, ulong? blobGasUsed, ulong? excessBlobGas)
    {
        ISpecProvider specProvider = new CustomSpecProvider(((ForkActivation)0, spec));
        HeaderValidator headerValidator = new(Substitute.For<IBlockTree>(), Always.Valid, specProvider, TestLogManager.Instance);
        BlockValidator blockValidator = new(Always.Valid, headerValidator, Always.Valid, specProvider, TestLogManager.Instance);
        BlockHeader parent = Build.A.BlockHeader.TestObject;
        return blockValidator.ValidateSuggestedBlock(
            Build.A.Block
                .WithBlobGasUsed(blobGasUsed)
                .WithExcessBlobGas(excessBlobGas)
                .WithWithdrawalsRoot(TestItem.KeccakA)
                .WithWithdrawals(TestItem.WithdrawalA_1Eth)
                .WithParent(parent)
                .TestObject,
            parent,
            out _);
    }

    [TestCaseSource(nameof(BlobsPerBlockCountTestCases))]
    public bool Blobs_per_block_count_is_valid(IReleaseSpec spec, ulong blobGasUsed)
    {
        ISpecProvider specProvider = new CustomSpecProvider(((ForkActivation)0, spec));
        BlockValidator blockValidator = new(Always.Valid, Always.Valid, Always.Valid, specProvider, TestLogManager.Instance);
        BlockHeader parent = Build.A.BlockHeader.TestObject;
        return blockValidator.ValidateSuggestedBlock(
            Build.A.Block
                .WithParent(parent)
                .WithWithdrawalsRoot(TestItem.KeccakA)
                .WithWithdrawals(TestItem.WithdrawalA_1Eth)
                .WithBlobGasUsed(blobGasUsed)
                .WithExcessBlobGas(0)
                .WithTransactions(Enumerable.Range(0, (int)(blobGasUsed / Sip4844Constants.GasPerBlob))
                    .Select(static i => Build.A.Transaction
                        .WithType(TxType.Blob)
                        .WithMaxFeePerBlobGas(ulong.MaxValue)
                        .WithBlobVersionedHashes(1).TestObject).ToArray())
                .TestObject,
            parent,
            out _);
    }

    private static IEnumerable<TestCaseData> BlobsPerBlockCountTestCases()
    {
        yield return new TestCaseData(SilaCancun.Instance, 0UL) { ExpectedResult = true };

        yield return new TestCaseData(SilaCancun.Instance, (SilaCancun.Instance.MaxBlobCount - 1) * Sip4844Constants.GasPerBlob) { ExpectedResult = true };

        yield return new TestCaseData(SilaCancun.Instance, SilaCancun.Instance.MaxBlobCount * Sip4844Constants.GasPerBlob) { ExpectedResult = true };

        yield return new TestCaseData(SilaCancun.Instance, (SilaCancun.Instance.MaxBlobCount + 1) * Sip4844Constants.GasPerBlob) { ExpectedResult = false };

        yield return new TestCaseData(SilaPrague.Instance, (SilaPrague.Instance.MaxBlobCount - 1) * Sip4844Constants.GasPerBlob) { ExpectedResult = true };

        yield return new TestCaseData(SilaPrague.Instance, (SilaPrague.Instance.MaxBlobCount) * Sip4844Constants.GasPerBlob) { ExpectedResult = true };

        yield return new TestCaseData(SilaPrague.Instance, (SilaPrague.Instance.MaxBlobCount + 1) * Sip4844Constants.GasPerBlob) { ExpectedResult = false };
    }

    private static IEnumerable<TestCaseData> BlobGasFieldsPerForkTestCases()
    {
        yield return new TestCaseData(SilaShanghai.Instance, null, null)
        {
            TestName = "Blob gas fields are not set pre-SilaCancun",
            ExpectedResult = true
        };
        yield return new TestCaseData(SilaShanghai.Instance, 0ul, null)
        {
            TestName = "BlobGasUsed is set pre-SilaCancun",
            ExpectedResult = false
        };
        yield return new TestCaseData(SilaShanghai.Instance, null, 0ul)
        {
            TestName = "ExcessBlobGas is set pre-SilaCancun",
            ExpectedResult = false
        };
        yield return new TestCaseData(SilaShanghai.Instance, 0ul, 0ul)
        {
            TestName = "Blob gas fields are set pre-SilaCancun",
            ExpectedResult = false
        };
        yield return new TestCaseData(SilaCancun.Instance, null, null)
        {
            TestName = "Blob gas fields are not set post-SilaCancun",
            ExpectedResult = false
        };
        yield return new TestCaseData(SilaCancun.Instance, 0ul, null)
        {
            TestName = "Just BlobGasUsed is set post-SilaCancun",
            ExpectedResult = false
        };
        yield return new TestCaseData(SilaCancun.Instance, null, 0ul)
        {
            TestName = "Just ExcessBlobGas is set post-SilaCancun",
            ExpectedResult = false
        };
        yield return new TestCaseData(SilaCancun.Instance, 0ul, 0ul)
        {
            TestName = "Blob gas fields are set post-SilaCancun",
            ExpectedResult = true
        };
    }
}
