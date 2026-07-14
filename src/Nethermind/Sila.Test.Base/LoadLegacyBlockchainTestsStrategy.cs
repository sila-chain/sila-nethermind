// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.IO;

namespace Sila.Test.Base;

public class LoadLegacyBlockchainTestsStrategy()
    : TestLoadStrategy(Path.Combine("LegacyTests", "SilaCancun", "BlockchainTests"), TestType.Blockchain)
{
    protected override SilaTest? HandleLoadFailure(string testFile, Exception e) =>
        new BlockchainTest { Name = testFile, LoadFailure = $"Failed to load: {e}" };
}
