// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Core
{
    public static class GasCostOf
    {
        public const ulong Free = 0;
        public const ulong Base = 2;
        public const ulong VeryLow = 3;
        public const ulong Low = 5;
        public const ulong Mid = 8;
        public const ulong High = 10;
        public const ulong Jump = Mid;
        public const ulong JumpI = High;
        public const ulong ExtCode = 20;
        public const ulong ExtCodeSip150 = 700;
        public const ulong Balance = 20;
        public const ulong BalanceSip150 = 400;
        public const ulong BalanceSip1884 = 700;
        public const ulong SLoad = 50;
        public const ulong SLoadSip150 = 200;
        public const ulong SLoadSip1884 = 800;
        public const ulong JumpDest = 1;
        public const ulong SStoreNetMeteredSip1283 = 200;
        public const ulong SStoreNetMeteredSip2200 = 800;
        public const ulong SSet = 20000;
        public const ulong SReset = 5000;
        public const ulong Create = 32000;
        public const ulong CodeDeposit = 200;
        public const ulong Call = 40;
        public const ulong CallSip150 = 700;
        public const ulong CallValue = 9000;
        public const ulong CallStipend = 2300;
        public const ulong NewAccount = 25000;
        public const ulong Exp = 10;
        public const ulong ExpByte = 10;
        public const ulong ExpByteSip160 = 50;
        public const ulong Memory = 3;
        public const ulong TxCreate = 32000;
        public const ulong TxDataZero = 4;
        public const ulong TxDataNonZero = 68;
        public const ulong TxDataNonZeroSip2028 = 16;
        public const ulong Transaction = 21000;
        public const ulong BlobHash = 3;
        public const ulong Log = 375;
        public const ulong LogTopic = 375;
        public const ulong LogData = 8;
        public const ulong Sha3 = 30;
        public const ulong Sha3Word = 6;
        public const ulong BlockHash = 20;
        public const ulong SelfDestruct = 0;
        public const ulong SelfDestructSip150 = 5000;
        public const ulong ExtCodeHash = 400;
        public const ulong ExtCodeHashSip1884 = 700;
        public const ulong SelfBalance = 5;
        public const ulong InitCodeWord = 2; //sip-3860 gas per word cost for init code size

        public const ulong ColdSLoad = 2100; // sip-2929

        public const ulong ColdAccountAccess = 2600; // sip-2929
        public const ulong WarmStateRead = 100; // sip-2929
        public const ulong CallPrecompileSip2929 = 100; // sip-2929

        public const ulong AccessAccountListEntry = 2400; // sip-2930
        public const ulong AccessStorageListEntry = 1900; // sip-2930
        public const ulong TLoad = WarmStateRead; // sip-1153
        public const ulong TStore = WarmStateRead; // sip-1153
        public const ulong PerAuthBaseCost = Sip7702Constants.PerAuthBaseCost;
        public const ulong TotalCostFloorPerTokenSip7623 = 10; // sip-7623
        public const ulong TotalCostFloorPerTokenSip7976 = 16; // sip-7976

        public const long CostPerStateByte = 1530; // sip-8037
        public const long StateBytesPerStorageSet = 64; // sip-8037
        public const long StateBytesPerNewAccount = 120; // sip-8037
        public const long StateBytesPerAuthBase = Sip8037Constants.StateBytesPerAuthBase;
        public const ulong SSetRegular = 2_900;
        public const long SSetState = StateBytesPerStorageSet * CostPerStateByte;
        public const ulong CreateRegular = 9_000;
        public const long CreateState = StateBytesPerNewAccount * CostPerStateByte;
        public const long NewAccountState = StateBytesPerNewAccount * CostPerStateByte;
        public const ulong CodeDepositRegularPerWord = 6;
        public const long CodeDepositState = CostPerStateByte;
        public const ulong PerAuthBaseRegular = Sip8037Constants.PerAuthBaseRegularCost;
        public const long PerAuthBaseState = StateBytesPerAuthBase * CostPerStateByte;
        public const ulong PerEmptyAccountState = StateBytesPerNewAccount * CostPerStateByte;
        public const ulong BlockAccessListItem = Sip7928Constants.ItemCost; // sip-7928

        public const ulong TxDataNonZeroMultiplier = TxDataNonZero / TxDataZero;
        public const ulong TxDataNonZeroMultiplierSip2028 = TxDataNonZeroSip2028 / TxDataZero;

        public const ulong MinModExpSip2565 = 200; // sip-2565
        public const ulong MinModExpSip7883 = 500; // sip-7883

        // sip-2780: reduce intrinsic transaction gas and reprice state-touching primitives.
        public const ulong TransactionSip2780 = 12000; // TX_BASE_COST: ECDSA recovery + sender account access + sender account write
        public const ulong TxValueCostSip2780 = 4244; // recipient balance write for a value-bearing transfer (non-create)
        public const ulong TransferLogSip2780 = 1756; // sip-7708 LOG3 transfer event cost
    }
}
