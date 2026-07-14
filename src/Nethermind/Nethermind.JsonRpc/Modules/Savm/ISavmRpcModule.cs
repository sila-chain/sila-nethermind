// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.JsonRpc.Modules.Savm
{
    [RpcModule(ModuleType.Savm)]
    public interface ISavmRpcModule : IRpcModule
    {
        [JsonRpcMethod(Description = "Triggers block production.", IsImplemented = true, IsSharable = false)]
        ResultWrapper<bool> savm_mine();
    }
}
