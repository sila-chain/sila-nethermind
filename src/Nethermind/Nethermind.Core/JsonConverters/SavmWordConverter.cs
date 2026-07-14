// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nethermind.Serialization.Json;

/// <summary>
/// Serializes an <see cref="SavmWord"/> (32 big-endian bytes) as a 0x-prefixed hex string with
/// leading zeros stripped, matching the canonical Sila JSON-RPC quantity format.
/// </summary>
public class SavmWordConverter : JsonConverter<SavmWord>
{
    public override SavmWord Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        SavmWord result = default;
        Span<byte> dest = MemoryMarshal.CreateSpan(ref Unsafe.As<SavmWord, byte>(ref result), 32);
        byte[]? bytes = ByteArrayConverter.Convert(ref reader);
        if (bytes is null || bytes.Length == 0)
        {
            return result;
        }
        if (bytes.Length > 32)
        {
            throw new JsonException("SavmWord hex value exceeds 32 bytes.");
        }
        bytes.AsSpan().CopyTo(dest[(32 - bytes.Length)..]);
        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        SavmWord value,
        JsonSerializerOptions options)
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.As<SavmWord, byte>(ref Unsafe.AsRef(in value)), 32);
        ByteArrayConverter.Convert(writer, bytes, skipLeadingZeros: true);
    }
}
