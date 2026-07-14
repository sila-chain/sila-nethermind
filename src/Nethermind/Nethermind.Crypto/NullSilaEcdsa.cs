// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Crypto;

namespace Nethermind.Crypto
{
    public class NullSilaEcdsa : ISilaEcdsa
    {
        public static NullSilaEcdsa Instance { get; } = new();

        public ulong ChainId => 0;

        private NullSilaEcdsa()
        {
        }

        public Signature Sign(PrivateKey privateKey, in ValueHash256 message) => throw new InvalidOperationException($"{nameof(NullSilaEcdsa)} does not expect any calls");

        public PublicKey RecoverPublicKey(Signature signature, in ValueHash256 message) => throw new InvalidOperationException($"{nameof(NullSilaEcdsa)} does not expect any calls");

        public CompressedPublicKey RecoverCompressedPublicKey(Signature signature, in ValueHash256 message) => throw new InvalidOperationException($"{nameof(NullSilaEcdsa)} does not expect any calls");

        public Address RecoverAddress(Transaction tx, bool useSignatureChainId = false) => throw new InvalidOperationException($"{nameof(NullSilaEcdsa)} does not expect any calls");

        public bool Verify(Address sender, Transaction tx) => throw new InvalidOperationException($"{nameof(NullSilaEcdsa)} does not expect any calls");

        public Address? RecoverAddress(Signature signature, in ValueHash256 message) => throw new InvalidOperationException($"{nameof(NullSilaEcdsa)} does not expect any calls");
    }
}
