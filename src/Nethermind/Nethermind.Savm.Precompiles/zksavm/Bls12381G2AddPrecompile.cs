// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Zkvm.Abstractions;

namespace Nethermind.Savm.Precompiles;

public partial class Bls12381G2AddPrecompile
{
    public partial Result<byte[]> Run(ReadOnlyMemory<byte> inputData, IReleaseSpec _)
    {
        if (!ValidateInputLength(inputData))
            return Errors.InvalidInputLength;

        Span<byte> decoded = stackalloc byte[Sip2537.LenG2Trimmed * 2];

        if (!Sip2537.TryDecodeG2(inputData.Span[..Sip2537.LenG2], decoded[..Sip2537.LenG2Trimmed]))
            return Errors.InvalidFieldElementTopBytes;

        if (!Sip2537.TryDecodeG2(inputData.Span[Sip2537.LenG2..], decoded[Sip2537.LenG2Trimmed..]))
            return Errors.InvalidFieldElementTopBytes;

        Span<byte> output = stackalloc byte[Sip2537.LenG2Trimmed];

        Accelerators.Status status = Accelerators.Bls12381G2Add(
            decoded[..Sip2537.LenG2Trimmed], decoded[Sip2537.LenG2Trimmed..], output);

        if (status == Accelerators.Status.OK)
        {
            byte[] encoded = new byte[Sip2537.LenG2];

            Sip2537.EncodeG2(output, encoded);

            return encoded;
        }

        return Errors.Failed;
    }
}
