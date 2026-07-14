// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.SilStats.Messages.Models;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Nethermind.SilStats.Messages
{
    public class HelloMessage(string secret, Info info) : IMessage
    {
        public string? Id { get; set; }
        public string Secret { get; } = secret;
        public Info Info { get; } = info;
    }
}
