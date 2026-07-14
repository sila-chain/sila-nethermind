// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Sila.Test.Base;

namespace Nethermind.SilaTests.Benchmark
{
    [ShortRunJob()]
    public class SilaTests : GeneralStateTestBase
    {
        public static IEnumerable<string> TestFileSource() => Directory.EnumerateFiles(@"SilaTestFiles", "*.json", SearchOption.AllDirectories);

        [Benchmark]
        [ArgumentsSource(nameof(TestFileSource))]
        public void Run(string testFile)
        {
            FileTestsSource source = new(testFile);
            IEnumerable<SilaTest> tests = source.LoadTests(TestType.State);

            foreach (GeneralStateTest test in tests)
            {
                RunTest(test);
            }
        }
    }
}
