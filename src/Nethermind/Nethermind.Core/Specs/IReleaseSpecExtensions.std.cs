// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Runtime.CompilerServices;

namespace Nethermind.Core.Specs;

public static partial class IReleaseSpecExtensions
{
    private static readonly ConditionalWeakTable<IReleaseSpec, IReleaseSpec> _noSip158Specs = [];
    private static readonly ConditionalWeakTable<IReleaseSpec, IReleaseSpec> _noSip3607Specs = [];

    private static IReleaseSpec GetNoSip158Spec(IReleaseSpec spec) =>
        _noSip158Specs.GetValue(spec, static s => new NoSip158Spec(s));

    private static IReleaseSpec GetNoSip3607Spec(IReleaseSpec spec) =>
        _noSip3607Specs.GetValue(spec, static s => new NoSip3607Spec(s));

    extension(IReleaseSpec spec)
    {
        public bool ClearEmptyAccountWhenTouched => spec.IsSip158Enabled;
        public bool UseHotAndColdStorage => spec.IsSip2929Enabled;
        public bool ChargeForTopLevelCreate => spec.IsSip2Enabled;
        public bool FailOnOutOfGasCodeDeposit => spec.IsSip2Enabled;
        public bool UseShanghaiDDosProtection => spec.IsSip150Enabled;
        public bool UseConstantinopleNetGasMetering => spec.IsSip1283Enabled;
        public bool UseIstanbulNetGasMetering => spec.IsSip2200Enabled;
        public bool UseNetGasMetering => spec.UseConstantinopleNetGasMetering || spec.UseIstanbulNetGasMetering;
        public bool UseNetGasMeteringWithAStipendFix => spec.UseIstanbulNetGasMetering;
        public bool Use63Over64Rule => spec.UseShanghaiDDosProtection;

        /// <summary>
        /// Determines whether the specified address is a precompiled contract for this release specification.
        /// </summary>
        /// <param name="address">The address to check for precompile status.</param>
        /// <returns><c>true</c> if the address is a precompiled contract; otherwise, <c>false</c>.</returns>
        public bool IsPrecompile(Address address) => spec.Precompiles.Contains(address);
    }
}
