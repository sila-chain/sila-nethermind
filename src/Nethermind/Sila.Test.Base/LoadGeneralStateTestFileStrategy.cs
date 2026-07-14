// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;

namespace Sila.Test.Base
{
    public class LoadGeneralStateTestFileStrategy : ITestLoadStrategy
    {
        public IEnumerable<SilaTest> Load(string testName, string? wildcard = null)
        {
            //in case user wants to give test file other than the ones in sila tests submodule
            if (File.Exists(testName))
            {
                FileTestsSource fileTestsSource = new(testName, wildcard);
                IEnumerable<SilaTest> tests = fileTestsSource.LoadTests(TestType.State);

                return tests;
            }

            string testsDirectory = GetGeneralStateTestsDirectory();

            IEnumerable<string> testFiles = Directory.EnumerateFiles(testsDirectory, testName, SearchOption.AllDirectories);

            List<SilaTest> generalStateTests = [];

            //load all tests from found test files in sila tests submodule
            foreach (string testFile in testFiles)
            {
                FileTestsSource fileTestsSource = new(testFile, wildcard);

                IEnumerable<SilaTest> tests = fileTestsSource.LoadTests(TestType.State);
                generalStateTests.AddRange(tests);
            }

            return generalStateTests;
        }

        private string GetGeneralStateTestsDirectory()
        {
            char pathSeparator = Path.AltDirectorySeparatorChar;
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            return Path.Combine(currentDirectory.Remove(currentDirectory.LastIndexOf("src", StringComparison.Ordinal)), "src", "tests", "GeneralStateTests");
        }
    }
}
