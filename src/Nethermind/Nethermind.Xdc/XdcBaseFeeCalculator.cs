// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Xdc;

internal class XdcBaseFeeCalculator : IBaseFeeCalculator
{
    //XDC 1559 just used a const value
    //https://github.com/XinFinOrg/XDPoSChain/blob/e324a78d9466c02a121d4931248f5dc9505b580a/consensus/misc/sip1559/sip1559.go#L56
    public const long BaseFee = 12500000000;

    public UInt256 Calculate(BlockHeader parent, ISip1559Spec specFor1559) => specFor1559.IsSip1559Enabled ? BaseFee : UInt256.Zero;
}
