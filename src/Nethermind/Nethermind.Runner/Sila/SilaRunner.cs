// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Nethermind.Api;
using Nethermind.Core;
using Nethermind.Core.ServiceStopper;
using Nethermind.Init.Steps;
using Nethermind.Logging;

namespace Nethermind.Runner.Sila;

public class SilaRunner(INethermindApi api, SilaStepsManager stepsManager, ILifetimeScope lifetimeScope, IServiceStopper serviceStopper)
{
    public INethermindApi Api => api;
    public ILifetimeScope LifetimeScope => lifetimeScope;
    private readonly ILogger _logger = api.LogManager.GetClassLogger<SilaRunner>();

    public async Task Start(CancellationToken cancellationToken)
    {
        if (_logger.IsDebug) _logger.Debug("Starting Sila runner");

        await stepsManager.InitializeAll(cancellationToken);

        string infoScreen = ThisNodeInfo.BuildNodeInfoScreen();

        if (_logger.IsInfo) _logger.Info(infoScreen);
    }

    public async Task StopAsync()
    {
        await serviceStopper.StopAllServices();

        await lifetimeScope.DisposeAsync();
        if (_logger.IsInfo)
        {
            _logger.Info("All DBs closed");
            _logger.Info("Sila runner stopped");
        }
    }
}
