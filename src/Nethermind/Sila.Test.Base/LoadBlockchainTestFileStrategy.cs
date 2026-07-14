// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;

namespace Sila.Test.Base
{
    public class LoadBlockchainTestFileStrategy : ITestLoadStrategy
    {
        public IEnumerable<SilaTest> Load(string testName, string? wildcard = null)
        {
            FileTestsSource fileTestsSource = new(testName, wildcard);
            return fileTestsSource.LoadTests(TestType.Blockchain);
        }
    }
}
