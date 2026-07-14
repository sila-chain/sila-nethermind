// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using Nethermind.Int256;

namespace Sila.Test.Base
{
    public class AccountState
    {
        public byte[] Code { get; set; } = [];
        public UInt256 Balance { get; set; }
        public ulong Nonce { get; set; }
        public Dictionary<UInt256, byte[]> Storage { get; set; } = [];
    }
}
