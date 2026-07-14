// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Core
{
    public static class RefundOf
    {
        public const ulong SSetReversedSip1283 = GasCostOf.SSet - GasCostOf.SStoreNetMeteredSip1283;
        public const ulong SResetReversedSip1283 = GasCostOf.SReset - GasCostOf.SStoreNetMeteredSip1283;
        public const ulong SSetReversedSip2200 = GasCostOf.SSet - GasCostOf.SStoreNetMeteredSip2200;
        public const ulong SResetReversedSip2200 = GasCostOf.SReset - GasCostOf.SStoreNetMeteredSip2200;
        public const ulong SSetReversedHotCold = GasCostOf.SSet - GasCostOf.WarmStateRead;
        public const ulong SResetReversedHotCold = GasCostOf.SReset - GasCostOf.ColdSLoad - GasCostOf.WarmStateRead;
        public const ulong SSetReversedSip8037 = GasCostOf.SSetState + GasCostOf.SSetRegular - GasCostOf.WarmStateRead;
        public const ulong SClearAfterSip3529 = GasCostOf.SReset - GasCostOf.ColdSLoad + GasCostOf.AccessStorageListEntry;
        public const ulong SClearSip8038 = (Sip8038Constants.StorageWrite + Sip8038Constants.ColdStorageAccess) * 4800 / 5000;
        public const ulong SClearBeforeSip3529 = 15000;
        public const ulong DestroyBeforeSip3529 = 24000;
        public const ulong DestroyAfterSip3529 = GasCostOf.Free;
    }
}
