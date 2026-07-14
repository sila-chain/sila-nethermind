// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Specs;
using Nethermind.Specs.Forks;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace Nethermind.Specs
{
    public class SpecNameParser
    {
        private static readonly ConcurrentDictionary<string, IReleaseSpec> _specs = new(StringComparer.Ordinal);

        public static IReleaseSpec Parse(string specName) => _specs.GetOrAdd(specName, ParseUncached);

        private static IReleaseSpec ParseUncached(string specName)
        {
            string unambiguousSpecName = new StringBuilder(specName)
                .Replace("SIP150", "TangerineWhistle")
                .Replace("SIP158", "SpuriousDragon")
                .Replace("DAO", "Dao")
                .Replace("Merged", "SilaParis")
                .Replace("Merge", "SilaParis")
                .Replace("GrayGlacier+3860", "SilaShanghai")
                .Replace("GrayGlacier+3855", "SilaShanghai")
                .Replace("SilaShanghai+3855", "SilaShanghai")
                .Replace("SilaShanghai+3860", "SilaShanghai")
                .Replace("GrayGlacier+1153", "SilaCancun")
                .Replace("Merge+1153", "SilaCancun")
                .Replace("SilaShanghai+6780", "SilaCancun")
                .Replace("GrayGlacier+1153", "SilaCancun")
                .Replace("Merge+1153", "SilaCancun")
                .ToString();

            return unambiguousSpecName switch
            {
                "Frontier" => Frontier.Instance,
                "Homestead" => Homestead.Instance,
                "TangerineWhistle" => TangerineWhistle.Instance,
                "SpuriousDragon" => SpuriousDragon.Instance,
                "SIP150" => TangerineWhistle.Instance,
                "SIP158" => SpuriousDragon.Instance,
                "Dao" => Dao.Instance,
                "Constantinople" => Constantinople.Instance,
                "ConstantinopleFix" => ConstantinopleFix.Instance,
                "Byzantium" => Byzantium.Instance,
                "Istanbul" => Istanbul.Instance,
                "Berlin" => Berlin.Instance,
                "London" => London.Instance,
                "ArrowGlacier" => ArrowGlacier.Instance,
                "GrayGlacier" => GrayGlacier.Instance,
                "SilaShanghai" => SilaShanghai.Instance,
                "SilaCancun" => SilaCancun.Instance,
                "SilaParis" => SilaParis.Instance,
                "SilaPrague" => SilaPrague.Instance,
                "SilaOsaka" => SilaOsaka.Instance,
                "BPO1" => BPO1.Instance,
                "BPO2" => BPO2.Instance,
                "BPO3" => BPO3.Instance,
                "BPO4" => BPO4.Instance,
                "BPO5" => BPO5.Instance,
                "SilaAmsterdam" => SilaAmsterdam.Instance,
                _ => throw new NotSupportedException()
            };
        }
    }
}
