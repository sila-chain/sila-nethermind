// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using NUnit.Framework;

namespace Nethermind.Core.Test;

/// <summary>Pins the SIP-8038 gas parameters and guards the derivation relationships.</summary>
public class Sip8038ConstantsTests
{
    [Test]
    public void Base_parameters_match_the_sip8038_values()
    {
        ulong coldAccountAccess = Sip8038Constants.ColdAccountAccess;
        ulong warmAccess = Sip8038Constants.WarmAccess;
        ulong coldStorageAccess = Sip8038Constants.ColdStorageAccess;
        ulong accountWrite = Sip8038Constants.AccountWrite;
        ulong storageWrite = Sip8038Constants.StorageWrite;
        ulong callStipend = Sip8038Constants.CallStipend;

        Assert.Multiple(() =>
        {
            Assert.That(coldAccountAccess, Is.EqualTo(3000));
            Assert.That(warmAccess, Is.EqualTo(100));
            Assert.That(coldStorageAccess, Is.EqualTo(3000));
            Assert.That(accountWrite, Is.EqualTo(8000));
            Assert.That(storageWrite, Is.EqualTo(10000));
            Assert.That(callStipend, Is.EqualTo(2300));
        });
    }

    [Test]
    public void Derived_parameters_match_the_sip8038_derivations() =>
        Assert.Multiple(() =>
        {
            Assert.That(Sip8038Constants.CallValue, Is.EqualTo(10300));
            Assert.That(Sip8038Constants.CreateAccess, Is.EqualTo(11000));
            Assert.That(RefundOf.SClearSip8038, Is.EqualTo(12480));
            Assert.That(Sip8038Constants.PerAuthBaseRegular, Is.EqualTo(15816));
        });

    [Test]
    public void Call_value_is_account_write_plus_stipend()
    {
        ulong callValue = Sip8038Constants.CallValue;
        Assert.That(callValue, Is.EqualTo(Sip8038Constants.AccountWrite + Sip8038Constants.CallStipend));
    }

    [Test]
    public void Create_access_is_account_write_plus_cold_storage_access()
    {
        ulong createAccess = Sip8038Constants.CreateAccess;
        Assert.That(createAccess, Is.EqualTo(Sip8038Constants.AccountWrite + Sip8038Constants.ColdStorageAccess));
    }

    [Test]
    public void Access_list_address_cost_equals_cold_account_access()
    {
        ulong addressCost = Sip8038Constants.AccessListAddressCost;
        Assert.That(addressCost, Is.EqualTo(Sip8038Constants.ColdAccountAccess));
    }

    [Test]
    public void Access_list_storage_key_cost_equals_cold_storage_access()
    {
        ulong storageKeyCost = Sip8038Constants.AccessListStorageKeyCost;
        Assert.That(storageKeyCost, Is.EqualTo(Sip8038Constants.ColdStorageAccess));
    }

    [Test]
    public void Access_list_costs_are_raised_above_the_sip2930_values()
    {
        ulong addressCost = Sip8038Constants.AccessListAddressCost;
        ulong storageKeyCost = Sip8038Constants.AccessListStorageKeyCost;

        Assert.Multiple(() =>
        {
            Assert.That(addressCost, Is.GreaterThan(GasCostOf.AccessAccountListEntry));
            Assert.That(storageKeyCost, Is.GreaterThan(GasCostOf.AccessStorageListEntry));
        });
    }

    [Test]
    public void Storage_clear_refund_follows_the_derivation_formula()
    {
        ulong storageClearRefund = RefundOf.SClearSip8038;
        ulong expected = (Sip8038Constants.StorageWrite + Sip8038Constants.ColdStorageAccess) * 4800 / 5000;
        Assert.That(storageClearRefund, Is.EqualTo(expected));
    }
}
