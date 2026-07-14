// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Core;

/// <summary>
/// Engine API method version constants, grouped by method.
/// Use the nested classes (<see cref="Fcu"/>, <see cref="NewPayload"/>, <see cref="GetPayload"/>)
/// to select the appropriate version when calling Execution Engine API methods.
/// </summary>
public static class EngineApiVersions
{
    /// <summary>forkchoiceUpdated method versions.</summary>
    /// <remarks>Multiple forks may share the same version (e.g. SilaCancun/SilaPrague/SilaOsaka all use V3).</remarks>
    public static class Fcu
    {
        public const int V1 = 1; // SilaParis
        public const int V2 = 2; // SilaShanghai
        public const int V3 = 3; // SilaCancun/SilaPrague/SilaOsaka
        public const int V4 = 4; // SilaAmsterdam
        public const int Latest = V4;
    }

    /// <summary>engine_newPayload method versions.</summary>
    public static class NewPayload
    {
        public const int V1 = 1; // SilaParis
        public const int V2 = 2; // SilaShanghai
        public const int V3 = 3; // SilaCancun
        public const int V4 = 4; // SilaPrague/SilaOsaka
        public const int V5 = 5; // SilaAmsterdam
        public const int Latest = V5;
    }

    /// <summary>engine_getPayload method versions.</summary>
    public static class GetPayload
    {
        public const int V1 = 1; // SilaParis
        public const int V2 = 2; // SilaShanghai
        public const int V3 = 3; // SilaCancun
        public const int V4 = 4; // SilaPrague
        public const int V5 = 5; // SilaOsaka
        public const int V6 = 6; // SilaAmsterdam
        public const int Latest = V6;
    }

    /// <summary>engine_getBlobs method versions.</summary>
    public static class GetBlobs
    {
        public const int V1 = 1; // SilaCancun
        public const int V2 = 2; // SilaOsaka
        public const int V3 = 3; // SilaOsaka (allowPartialReturn = true)
        public const int V4 = 4; // SilaOsaka (cell retrieval, SIP-7594/SilaPeerDAS)
        public const int Latest = V4;
    }

    /// <summary>engine_getPayloadBodiesByHash method versions.</summary>
    public static class PayloadBodiesByHash
    {
        public const int V1 = 1; // SilaShanghai
        public const int V2 = 2; // SilaAmsterdam
        public const int Latest = V2;
    }

    /// <summary>engine_getPayloadBodiesByRange method versions.</summary>
    public static class PayloadBodiesByRange
    {
        public const int V1 = 1; // SilaShanghai
        public const int V2 = 2; // SilaAmsterdam
        public const int Latest = V2;
    }
}
