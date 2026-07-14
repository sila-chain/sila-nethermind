// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using BenchmarkDotNet.Attributes;

namespace Nethermind.Benchmarks.Savm
{
    public class ECRecoverBenchmark
    {
        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public bool Improved() => throw new NotImplementedException();

        [Benchmark]
        public bool Current() => throw new NotImplementedException();
    }
}
