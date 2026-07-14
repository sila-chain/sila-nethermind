// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.SilStats.Configs
{
    public class SilStatsConfig : ISilStatsConfig
    {
        public bool Enabled { get; set; }
        public string? Server { get; set; } = "ws://localhost:3000/api";
        public string? Name { get; set; } = "Nethermind";
        public string? Secret { get; set; } = "secret";
        public string? Contact { get; set; } = "hello@nethermind.io";
        public int SendInterval { get; set; } = 15;
    }
}
