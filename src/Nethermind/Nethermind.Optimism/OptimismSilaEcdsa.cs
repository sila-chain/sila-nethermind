// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Crypto;

namespace Nethermind.Optimism;

public class OptimismSilaEcdsa(ISilaEcdsa silaEcdsa) : Ecdsa, ISilaEcdsa
{
    private readonly ISilaEcdsa _silaEcdsa = silaEcdsa;

    public ulong ChainId => _silaEcdsa.ChainId;

    public Address? RecoverAddress(Signature signature, in ValueHash256 message) => _silaEcdsa.RecoverAddress(signature, in message);
}
