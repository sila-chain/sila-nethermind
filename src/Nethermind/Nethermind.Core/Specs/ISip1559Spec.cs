// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Int256;

namespace Nethermind.Core.Specs
{
    /// <summary>
    /// https://github.com/sila-chain/SIPs
    /// </summary>
    public interface ISip1559Spec
    {
        /// <summary>
        /// Gas target and base fee, and fee burning.
        /// </summary>
        bool IsSip1559Enabled { get; }
        public ulong Sip1559TransitionBlock { get; }
        // Collects for both SIP-1559 and SIP-4844-Pectra
        public Address? FeeCollector => null;
        public UInt256? Sip1559BaseFeeMinValue => null;
        public UInt256 ForkBaseFee { get; }
        public UInt256 BaseFeeMaxChangeDenominator { get; }
        public ulong ElasticityMultiplier { get; }
        public IBaseFeeCalculator BaseFeeCalculator { get; }
    }

    public sealed class OverridableSip1559Spec(ISip1559Spec spec) : ISip1559Spec
    {
        public bool IsSip1559Enabled { get; init; } = spec.IsSip1559Enabled;
        public ulong Sip1559TransitionBlock { get; init; } = spec.Sip1559TransitionBlock;
        public Address? FeeCollector { get; init; } = spec.FeeCollector;
        public UInt256? Sip1559BaseFeeMinValue { get; init; } = spec.Sip1559BaseFeeMinValue;
        public UInt256 ForkBaseFee { get; init; } = spec.ForkBaseFee;
        public UInt256 BaseFeeMaxChangeDenominator { get; init; } = spec.BaseFeeMaxChangeDenominator;
        public ulong ElasticityMultiplier { get; init; } = spec.ElasticityMultiplier;
        public IBaseFeeCalculator BaseFeeCalculator { get; init; } = spec.BaseFeeCalculator;
    }
}
