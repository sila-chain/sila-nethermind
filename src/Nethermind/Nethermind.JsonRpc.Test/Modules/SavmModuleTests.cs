// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Threading.Tasks;
using Nethermind.Consensus.Producers;
using Nethermind.JsonRpc.Modules.Savm;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.JsonRpc.Test.Modules
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class SavmModuleTests
    {
        [Test]
        public async Task Savm_mine()
        {
            IManualBlockProductionTrigger trigger = Substitute.For<IManualBlockProductionTrigger>();
            SavmRpcModule rpcModule = new(trigger);
            string response = await RpcTest.TestSerializedRequest<ISavmRpcModule>(rpcModule, "savm_mine");
            Assert.That(response, Is.EqualTo("{\"jsonrpc\":\"2.0\",\"result\":true,\"id\":67}"));
            await trigger.Received().BuildBlock();
        }
    }
}
