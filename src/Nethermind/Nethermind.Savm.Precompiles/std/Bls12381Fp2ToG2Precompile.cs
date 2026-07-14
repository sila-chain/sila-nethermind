// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Runtime.CompilerServices;
using Nethermind.Core;
using Nethermind.Core.Specs;
using G2 = Nethermind.Crypto.Bls.P2;

namespace Nethermind.Savm.Precompiles;

public partial class Bls12381Fp2ToG2Precompile
{
    [SkipLocalsInit]
    public partial Result<byte[]> Run(ReadOnlyMemory<byte> inputData, IReleaseSpec _)
    {
        Metrics.Bls12381Fp2ToG2Precompile++;

        if (!ValidateInputLength(inputData))
            return Errors.InvalidInputLength;

        G2 res = new(stackalloc long[G2.Sz]);
        Result result =
            Sip2537.ValidRawFp(inputData.Span[..Sip2537.LenFp]) &&
            Sip2537.ValidRawFp(inputData.Span[Sip2537.LenFp..]);

        if (result)
        {
            // map field point to G2
            ReadOnlySpan<byte> fp0 = inputData[Sip2537.LenFpPad..Sip2537.LenFp].Span;
            ReadOnlySpan<byte> fp1 = inputData[(Sip2537.LenFp + Sip2537.LenFpPad)..].Span;
            res.MapTo(fp0, fp1);

            return res.EncodeRaw();
        }

        return result.Error!;
    }
}
