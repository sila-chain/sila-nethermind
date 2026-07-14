// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Frozen;
using Nethermind.Core;
using Nethermind.Savm.CodeAnalysis;

namespace Nethermind.Savm;

public interface IPrecompileProvider
{
    public FrozenDictionary<AddressAsKey, CodeInfo> GetPrecompiles();
}
