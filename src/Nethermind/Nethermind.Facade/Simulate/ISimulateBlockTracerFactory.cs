// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Savm.State;
using Nethermind.Savm.Tracing;

namespace Nethermind.Facade.Simulate;

public interface ISimulateBlockTracerFactory<out TTrace>
{
    public IBlockTracer<TTrace> CreateSimulateBlockTracer(bool isTracingLogs, IWorldState worldState, ISpecProvider spec, BlockHeader block);
}
