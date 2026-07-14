// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Extensions;
using Nethermind.Core.Test.Builders;
using Nethermind.Savm;
using Nethermind.Savm.Test;
using Nethermind.Test.Runner;
using NUnit.Framework;

namespace Nethermind.State.Test.Runner.Test;

public class StateTestTxTracerTest : VirtualMachineTestsBase
{
    private StateTestTxTracer tracer;

    [SetUp]
    public void StateTestTxTracerSetUp() => tracer = new StateTestTxTracer();

    [TearDown]
    public void StateTestTxTracerTearDown() => tracer.Dispose();

    [Test]
    public void Does_not_throw_on_call()
    {
        byte[] code = Prepare.SavmCode
            .CallWithValue(TestItem.AddressC, 50000, 1000000.Sila)
            .Done;

        Assert.DoesNotThrow(() => Execute(tracer, code));
    }
}
