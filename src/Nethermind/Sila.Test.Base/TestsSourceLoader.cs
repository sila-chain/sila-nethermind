// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Nethermind.Core.Test;

namespace Sila.Test.Base
{
    public class TestsSourceLoader(ITestLoadStrategy testLoadStrategy, string path, string? wildcard = null)
        : ITestSourceLoader
    {
        private readonly ITestLoadStrategy _testLoadStrategy = testLoadStrategy ?? throw new ArgumentNullException(nameof(testLoadStrategy));
        private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));

        public IEnumerable<SilaTest> LoadTests() =>
            TestChunkFilter.FilterByChunk(_testLoadStrategy.Load(_path, wildcard));

        public IEnumerable<TTestType> LoadTests<TTestType>() where TTestType : SilaTest =>
            TestChunkFilter.FilterByChunk(_testLoadStrategy.Load(_path, wildcard).OfType<TTestType>());
    }
}
