// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Specs.Forks;
using Nethermind.Specs.Test;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

[TestFixture]
public class Sip7976Tests
{
    private static readonly IReleaseSpec Sip7976Spec = new OverridableReleaseSpec(SilaPrague.Instance) { IsSip7976Enabled = true };

    // SIP-7976 floor: 21000 + byteCount * STANDARD_TOKEN_COST(4) * TOTAL_COST_FLOOR_PER_TOKEN(16)
    // SIP-7623 floor: 21000 + tokens * 10  (tokens = zeros + nonzeros * STANDARD_TOKEN_COST)
    // Standard: 21000 + zero*4 + nonzero*16 (+ 32000 + InitCodeWord for contract creation)
    private static IEnumerable<TestCaseData> IntrinsicGasCases()
    {
        yield return new TestCaseData(Sip7976Spec, Address.Zero, new byte[] { 1 }, 21_016UL, 21_064UL)
            .SetName("SIP-7976: single nonzero byte");
        yield return new TestCaseData(Sip7976Spec, Address.Zero, new byte[] { 0 }, 21_004UL, 21_064UL)
            .SetName("SIP-7976: single zero byte");
        yield return new TestCaseData(Sip7976Spec, Address.Zero, new byte[] { 0, 0, 1, 1 }, 21_040UL, 21_256UL)
            .SetName("SIP-7976: mixed data");
        yield return new TestCaseData(Sip7976Spec, Address.Zero, Array.Empty<byte>(), 21_000UL, 21_000UL)
            .SetName("SIP-7976: empty data");
        yield return new TestCaseData(Sip7976Spec, null, new byte[] { 1 }, 53_018UL, 21_064UL)
            .SetName("SIP-7976: contract creation standard exceeds floor");
        yield return new TestCaseData(SilaPrague.Instance, Address.Zero, new byte[] { 0 }, 21_004UL, 21_010UL)
            .SetName("SIP-7623 fallback: single zero byte");
    }

    [TestCaseSource(nameof(IntrinsicGasCases))]
    public void Intrinsic_gas_calculation(IReleaseSpec spec, Address? to, byte[] data, ulong expectedStandard, ulong expectedFloor)
    {
        Transaction transaction = new() { Data = data, To = to };
        SilaIntrinsicGas cost = IntrinsicGasCalculator.Calculate(transaction, spec);
        Assert.That(cost, Is.EqualTo(new SilaIntrinsicGas(expectedStandard, expectedFloor)));
    }
}
