// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Api;
using Nethermind.Api.Extensions;
using Nethermind.SilStats.Configs;
using Nethermind.Runner.Test.Sila;
using NUnit.Framework;

namespace Nethermind.SilStats.Test;

public class SilStatsPluginTests
{
    private NethermindApi _context = null!;
    private INethermindPlugin _plugin = null!;

    [SetUp]
    public void Setup() => _context = Build.ContextWithMocks();

    [Test]
    public void Init_eth_stats_plugin_does_not_throw_exception([Values] bool enabled)
    {
        _plugin = new SilStatsPlugin(new SilStatsConfig() { Enabled = enabled });

        Assert.DoesNotThrow(() => _plugin.InitTxTypesAndRlpDecoders(_context));
    }
}
