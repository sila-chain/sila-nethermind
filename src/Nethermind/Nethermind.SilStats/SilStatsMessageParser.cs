// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Nethermind.SilStats;

internal enum SilStatsIncomingMessageType
{
    Unknown,
    History,
    NodePing,
    NodePong
}

internal readonly record struct SilStatsHistoryRequest(ulong Min, ulong Max);

internal readonly record struct SilStatsNodeTiming(long? ClientTime);

internal readonly record struct SilStatsIncomingMessage(
    string EventTypeName,
    SilStatsIncomingMessageType MessageType,
    SilStatsHistoryRequest? HistoryRequest,
    SilStatsNodeTiming? NodeTiming);

internal static class SilStatsMessageParser
{
    private const string History = "history";
    private const string NodePing = "node-ping";
    private const string NodePong = "node-pong";
    private const int StackBufferThreshold = 512;

    [SkipLocalsInit]
    public static bool TryParse(string? message, out SilStatsIncomingMessage incomingMessage)
    {
        incomingMessage = new SilStatsIncomingMessage(string.Empty, SilStatsIncomingMessageType.Unknown, null, null);

        if (string.IsNullOrWhiteSpace(message) || message[0] != '{')
        {
            return false;
        }

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(message.Length);
        byte[]? rented = null;
        Span<byte> buffer = maxByteCount <= StackBufferThreshold
            ? stackalloc byte[StackBufferThreshold]
            : (rented = ArrayPool<byte>.Shared.Rent(maxByteCount));

        try
        {
            int written = Encoding.UTF8.GetBytes(message, buffer);
            return TryParseCore(buffer[..written], out incomingMessage);
        }
        catch (JsonException)
        {
            return false;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static bool TryParseCore(ReadOnlySpan<byte> utf8Bytes, out SilStatsIncomingMessage incomingMessage)
    {
        incomingMessage = new SilStatsIncomingMessage(string.Empty, SilStatsIncomingMessageType.Unknown, null, null);

        Utf8JsonReader reader = new(utf8Bytes);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return false;
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return false;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                return false;
            }

            if (!reader.ValueTextEquals("emit"u8))
            {
                reader.Skip();
                continue;
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            {
                return false;
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
            {
                return false;
            }

            string eventType;
            SilStatsIncomingMessageType messageType;

            if (reader.ValueTextEquals("history"u8))
            {
                eventType = History;
                messageType = SilStatsIncomingMessageType.History;
            }
            else if (reader.ValueTextEquals("node-ping"u8))
            {
                eventType = NodePing;
                messageType = SilStatsIncomingMessageType.NodePing;
            }
            else if (reader.ValueTextEquals("node-pong"u8))
            {
                eventType = NodePong;
                messageType = SilStatsIncomingMessageType.NodePong;
            }
            else
            {
                eventType = reader.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(eventType))
                {
                    return false;
                }
                messageType = SilStatsIncomingMessageType.Unknown;
            }

            if (!reader.Read())
            {
                return false;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
            {
                if (messageType == SilStatsIncomingMessageType.History)
                {
                    return false;
                }

                SilStatsNodeTiming? emptyTiming = messageType is SilStatsIncomingMessageType.NodePing or SilStatsIncomingMessageType.NodePong
                    ? new SilStatsNodeTiming(null)
                    : null;
                incomingMessage = new SilStatsIncomingMessage(eventType, messageType, null, emptyTiming);
                return true;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return false;
            }

            switch (messageType)
            {
                case SilStatsIncomingMessageType.History:
                    {
                        if (!TryReadHistoryRequest(ref reader, out SilStatsHistoryRequest historyRequest))
                        {
                            return false;
                        }

                        incomingMessage = new SilStatsIncomingMessage(eventType, SilStatsIncomingMessageType.History, historyRequest, null);
                        return true;
                    }
                case SilStatsIncomingMessageType.NodePing:
                case SilStatsIncomingMessageType.NodePong:
                    {
                        SilStatsNodeTiming timing = ReadNodeTiming(ref reader);
                        incomingMessage = new SilStatsIncomingMessage(eventType, messageType, null, timing);
                        return true;
                    }
                default:
                    {
                        incomingMessage = new SilStatsIncomingMessage(eventType, SilStatsIncomingMessageType.Unknown, null, null);
                        return true;
                    }
            }
        }

        return false;
    }

    private static bool TryReadHistoryRequest(ref Utf8JsonReader reader, out SilStatsHistoryRequest historyRequest)
    {
        historyRequest = default;
        ulong? min = null;
        ulong? max = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                return false;
            }

            bool isMin = reader.ValueTextEquals("min"u8);
            bool isMax = !isMin && reader.ValueTextEquals("max"u8);

            if (!reader.Read())
            {
                return false;
            }

            if (isMin)
            {
                if (!TryReadUInt64(ref reader, out ulong value))
                {
                    return false;
                }
                min = value;
            }
            else if (isMax)
            {
                if (!TryReadUInt64(ref reader, out ulong value))
                {
                    return false;
                }
                max = value;
            }
            else
            {
                reader.Skip();
            }
        }

        if (min is null || max is null)
        {
            return false;
        }

        historyRequest = new SilStatsHistoryRequest(min.Value, max.Value);
        return true;
    }

    private static SilStatsNodeTiming ReadNodeTiming(ref Utf8JsonReader reader)
    {
        long? clientTime = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                break;
            }

            bool isClientTime = reader.ValueTextEquals("clientTime"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isClientTime && TryReadInt64(ref reader, out long value))
            {
                clientTime = value;
            }
            else if (!isClientTime)
            {
                reader.Skip();
            }
        }

        return new SilStatsNodeTiming(clientTime);
    }

    private static bool TryReadUInt64(ref Utf8JsonReader reader, out ulong value)
    {
        value = 0;

        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.TryGetUInt64(out value);
            case JsonTokenType.String:
                return TryParseUInt64String(ref reader, out value);
            default:
                return false;
        }
    }

    private static bool TryReadInt64(ref Utf8JsonReader reader, out long value)
    {
        value = 0;

        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.TryGetInt64(out value);
            case JsonTokenType.String:
                return TryParseInt64String(ref reader, out value);
            default:
                return false;
        }
    }

    [SkipLocalsInit]
    private static bool TryParseUInt64String(ref Utf8JsonReader reader, out ulong value)
    {
        // An unsigned 64-bit integer is at most 20 ASCII bytes ("18446744073709551615")
        const int maxUInt64Digits = 20;

        if (reader.HasValueSequence)
        {
            long sequenceLength = reader.ValueSequence.Length;
            if (sequenceLength > maxUInt64Digits)
            {
                value = 0;
                return false;
            }

            Span<byte> sequenceBuffer = stackalloc byte[maxUInt64Digits];
            reader.ValueSequence.CopyTo(sequenceBuffer);
            ReadOnlySpan<byte> sequenceRaw = sequenceBuffer[..(int)sequenceLength];
            return Utf8Parser.TryParse(sequenceRaw, out value, out int sequenceConsumed) && sequenceConsumed == sequenceRaw.Length;
        }

        ReadOnlySpan<byte> raw = reader.ValueSpan;
        return Utf8Parser.TryParse(raw, out value, out int consumed) && consumed == raw.Length;
    }

    [SkipLocalsInit]
    private static bool TryParseInt64String(ref Utf8JsonReader reader, out long value)
    {
        // A signed 64-bit integer is at most 20 ASCII bytes ("-9223372036854775808"); anything
        // longer cannot be a valid long, so we can reject without allocating.
        const int maxInt64Digits = 20;

        if (reader.HasValueSequence)
        {
            long sequenceLength = reader.ValueSequence.Length;
            if (sequenceLength > maxInt64Digits)
            {
                value = 0;
                return false;
            }

            Span<byte> sequenceBuffer = stackalloc byte[maxInt64Digits];
            reader.ValueSequence.CopyTo(sequenceBuffer);
            ReadOnlySpan<byte> sequenceRaw = sequenceBuffer[..(int)sequenceLength];
            return Utf8Parser.TryParse(sequenceRaw, out value, out int sequenceConsumed) && sequenceConsumed == sequenceRaw.Length;
        }

        ReadOnlySpan<byte> raw = reader.ValueSpan;
        return Utf8Parser.TryParse(raw, out value, out int consumed) && consumed == raw.Length;
    }
}
