// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Specs;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

[TestFixture]
public class Sip7623Tests : VirtualMachineTestsBase
{
    protected override ulong BlockNumber => MainnetSpecProvider.ParisBlockNumber;
    protected override ulong Timestamp => MainnetSpecProvider.PragueBlockTimestamp;

    [Test]
    public void non_zero_data_transaction_floor_cost_should_be_40()
    {
        Transaction transaction = new() { Data = new byte[] { 1 }, To = Address.Zero };
        SilaIntrinsicGas cost = IntrinsicGasCalculator.Calculate(transaction, Spec);
        Assert.That(cost, Is.EqualTo(new SilaIntrinsicGas(Standard: GasCostOf.Transaction + GasCostOf.TxDataNonZeroSip2028,
            FloorGas: GasCostOf.Transaction + Spec.GasCosts.TotalCostFloorPerToken * 4)));
    }

    [Test]
    public void zero_data_transaction_floor_cost_should_be_10()
    {
        Transaction transaction = new() { Data = new byte[] { 0 }, To = Address.Zero };
        SilaIntrinsicGas cost = IntrinsicGasCalculator.Calculate(transaction, Spec);
        Assert.That(cost, Is.EqualTo(new SilaIntrinsicGas(Standard: GasCostOf.Transaction + GasCostOf.TxDataZero,
            FloorGas: GasCostOf.Transaction + Spec.GasCosts.TotalCostFloorPerToken)));
    }
}
