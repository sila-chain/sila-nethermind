// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Specs;

namespace Nethermind.Core;

public static class Sip7825Constants
{
    public static readonly ulong DefaultTxGasLimitCap = 16_777_216;
    public static ulong GetTxGasLimitCap(this IReleaseSpec spec)
        => spec.IsSip7825Enabled && !spec.IsSip8037Enabled
            ? DefaultTxGasLimitCap
            : ulong.MaxValue;
}
