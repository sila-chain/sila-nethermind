// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.SilStats.Messages.Models;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Nethermind.SilStats.Messages
{
    public class PendingMessage(PendingStats stats) : IMessage
    {
        public string? Id { get; set; }
        public PendingStats Stats { get; } = stats;
    }
}
