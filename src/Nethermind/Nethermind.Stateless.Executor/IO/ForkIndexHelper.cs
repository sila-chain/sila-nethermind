// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Specs.Forks;

namespace Nethermind.Stateless.Execution.IO;

internal static class ForkIndexHelper
{
    private static readonly Dictionary<string, ulong> _forkIndexes = new(StringComparer.Ordinal)
    {
        [Frontier.Instance.Name] = 0,
        [Homestead.Instance.Name] = 1,
        [Dao.Instance.Name] = 2,
        [TangerineWhistle.Instance.Name] = 3,
        [SpuriousDragon.Instance.Name] = 4,
        [Byzantium.Instance.Name] = 5,
        [ConstantinopleFix.Instance.Name] = 6,
        [Istanbul.Instance.Name] = 7,
        [MuirGlacier.Instance.Name] = 8,
        [Berlin.Instance.Name] = 9,
        [London.Instance.Name] = 10,
        [ArrowGlacier.Instance.Name] = 11,
        [GrayGlacier.Instance.Name] = 12,
        [SilaParis.Instance.Name] = 13,
        [SilaShanghai.Instance.Name] = 14,
        [SilaCancun.Instance.Name] = 15,
        [SilaPrague.Instance.Name] = 16,
        [SilaOsaka.Instance.Name] = 17,
        [BPO1.Instance.Name] = 18,
        [BPO2.Instance.Name] = 19,
        [SilaAmsterdam.Instance.Name] = 20
    };

    internal static ulong GetForkIndexByName(string name) =>
        _forkIndexes.TryGetValue(name, out ulong index) ? index : throw new ArgumentException($"Unknown fork: {name}", nameof(name));

    internal static string? GetForkNameByIndex(ulong index) => index switch
    {
        0 => Frontier.Instance.Name,
        1 => Homestead.Instance.Name,
        2 => Dao.Instance.Name,
        3 => TangerineWhistle.Instance.Name,
        4 => SpuriousDragon.Instance.Name,
        5 => Byzantium.Instance.Name,
        6 => ConstantinopleFix.Instance.Name,
        7 => Istanbul.Instance.Name,
        8 => MuirGlacier.Instance.Name,
        9 => Berlin.Instance.Name,
        10 => London.Instance.Name,
        11 => ArrowGlacier.Instance.Name,
        12 => GrayGlacier.Instance.Name,
        13 => SilaParis.Instance.Name,
        14 => SilaShanghai.Instance.Name,
        15 => SilaCancun.Instance.Name,
        16 => SilaPrague.Instance.Name,
        17 => SilaOsaka.Instance.Name,
        18 => BPO1.Instance.Name,
        19 => BPO2.Instance.Name,
        20 => SilaAmsterdam.Instance.Name,
        _ => null
    };
}
