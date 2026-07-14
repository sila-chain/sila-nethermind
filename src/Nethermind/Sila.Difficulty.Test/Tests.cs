// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sila.Test.Base;
using Nethermind.Specs;
using Nethermind.Specs.Forks;
using NUnit.Framework;

namespace Sila.Difficulty.Test;

[TestFixture]
public class MetaTests
{
    [Test]
    public void All_categories_are_tested() =>
        TestDirectoryHelper.AssertAllCategoriesTested(GetType(),
            Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(f => f.StartsWith("difficulty") && !f.StartsWith("difficultyRopsten")),
            ExpectedTypeName);

    private static string ExpectedTypeName(string fileName)
    {
        string name = fileName;
        if (!name.EndsWith("Tests"))
            name = name.EndsWith("Test") ? name + "s" : name + "Tests";
        return name.Replace("_", string.Empty);
    }
}

public class DifficultyCustomHomesteadTests()
    : DifficultyHexTestFixture<DifficultyCustomHomesteadTests>(new TestSingleReleaseSpecProvider(Homestead.Instance));

public class DifficultyCustomMainNetworkTests()
    : DifficultyHexTestFixture<DifficultyCustomMainNetworkTests>(MainnetSpecProvider.Instance);

public class DifficultyFrontierTests()
    : DifficultyHexTestFixture<DifficultyFrontierTests>(new TestSingleReleaseSpecProvider(Frontier.Instance));

public class DifficultyHomesteadTests()
    : DifficultyHexTestFixture<DifficultyHomesteadTests>(new TestSingleReleaseSpecProvider(Homestead.Instance));

public class DifficultyByzantiumTests()
    : DifficultyHexTestFixture<DifficultyByzantiumTests>(new TestSingleReleaseSpecProvider(Byzantium.Instance));

public class DifficultyConstantinopleTests()
    : DifficultyHexTestFixture<DifficultyConstantinopleTests>(new TestSingleReleaseSpecProvider(Constantinople.Instance));

public class DifficultySIP2384Tests()
    : DifficultyHexTestFixture<DifficultySIP2384Tests>(new TestSingleReleaseSpecProvider(MuirGlacier.Instance));

public class DifficultySIP2384_randomTests()
    : DifficultyHexTestFixture<DifficultySIP2384_randomTests>(new TestSingleReleaseSpecProvider(MuirGlacier.Instance));

public class DifficultySIP2384_random_to20MTests()
    : DifficultyHexTestFixture<DifficultySIP2384_random_to20MTests>(new TestSingleReleaseSpecProvider(MuirGlacier.Instance));

[Parallelizable(ParallelScope.All)]
public class DifficultyMainNetworkTests : TestsBase
{
    public static IEnumerable<DifficultyTests> LoadBasicTests() => Load("difficulty.json");

    public static IEnumerable<DifficultyTests> LoadMainTests() => LoadHex("difficultyMainNetwork.json");

    [TestCaseSource(nameof(LoadBasicTests))]
    public void Test_basic(DifficultyTests test) => RunTest(test, MainnetSpecProvider.Instance);

    [TestCaseSource(nameof(LoadMainTests))]
    public void Test_main(DifficultyTests test) => RunTest(test, MainnetSpecProvider.Instance);
}
