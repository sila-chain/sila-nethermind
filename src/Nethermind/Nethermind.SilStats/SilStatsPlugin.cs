// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Autofac;
using Autofac.Core;
using Nethermind.Api.Extensions;
using Nethermind.Api.Steps;

namespace Nethermind.SilStats;

public class SilStatsPlugin(ISilStatsConfig silStatsConfig) : INethermindPlugin
{
    public bool Enabled => silStatsConfig.Enabled;
    public string Name => "SilStats";
    public string Description => "Sila Statistics";
    public string Author => "Nethermind";
    public IModule Module => new SilStatsModule();
}

public class SilStatsModule : Module
{
    protected override void Load(ContainerBuilder builder) => builder.AddStep(typeof(SilStatsStep));
}
