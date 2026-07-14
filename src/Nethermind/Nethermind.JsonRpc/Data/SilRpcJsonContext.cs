// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Text.Json.Serialization;
using Nethermind.Blockchain.Tracing.GethStyle;
using Nethermind.Blockchain.Tracing.ParityStyle;
using Nethermind.Savm;
using Nethermind.State.Proofs;

namespace Nethermind.JsonRpc.Data;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// sil_ types
[JsonSerializable(typeof(AccountOverride))]
[JsonSerializable(typeof(AccountProof))]
// debug_ types
[JsonSerializable(typeof(GethLikeTxTrace))]
[JsonSerializable(typeof(GethTraceOptions))]
// trace_ types
[JsonSerializable(typeof(ParityLikeTxTrace))]
public partial class SilRpcJsonContext : JsonSerializerContext;
