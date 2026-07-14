// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;

namespace Nethermind.Synchronization
{
    public class SilSyncException : Exception
    {
        public SilSyncException(string message) : base(message)
        {
        }

        public SilSyncException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
