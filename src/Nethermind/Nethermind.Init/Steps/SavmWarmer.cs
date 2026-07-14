// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Nethermind.Api.Steps;
using Nethermind.Core;
using Nethermind.Savm;
using Nethermind.Savm.State;
using Nethermind.Init.Steps;
using Nethermind.State.OverridableEnv;

[RunnerStepDependencies(
    typeof(InitializeBlockchain)
)]
public class SavmWarmer(IOverridableEnvFactory envFactory, ILifetimeScope rootScope) : IStep
{
    public Task Execute(CancellationToken cancellationToken)
    {
        IOverridableEnv env = envFactory.Create();
        using IDisposable envScope = env.BuildAndOverride(null, null);

        using ILifetimeScope childContainerScope = rootScope.BeginLifetimeScope((builder) =>
        {
            builder.AddModule(env);
        });

        SilaVirtualMachine.WarmUpSavmInstructions(childContainerScope.Resolve<IWorldState>(), childContainerScope.Resolve<ICodeInfoRepository>());

        return Task.CompletedTask;
    }
}
