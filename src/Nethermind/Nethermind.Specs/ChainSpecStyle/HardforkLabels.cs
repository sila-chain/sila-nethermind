// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Nethermind.Config;
using Nethermind.Core.Exceptions;
using Nethermind.Core.Extensions;

namespace Nethermind.Specs.ChainSpecStyle;

/// <summary>
/// Shorthand hardfork labels for the Parity-style chainspec JSON and the SIP-7949 Geth-style
/// genesis. Each label is a single activation value — a block number for pre-SilaShanghai forks
/// (<c>ulong?</c>) and a timestamp for post-merge forks (<c>ulong?</c>) — that fans out at load
/// time to the full set of per-SIP transition fields that make up that fork.
/// </summary>
/// <remarks>
/// Labels are sourced from <see cref="IHasNamedForks"/> — populated by <c>[JsonExtensionData]</c>
/// on the Parity side and by typed <c>&lt;fork&gt;Time</c> / <c>&lt;fork&gt;Block</c> property
/// setters on the Geth side — and applied to an <see cref="ChainParameters"/> target. A
/// label and an explicit per-SIP field may coexist with matching values (redundant); conflicting
/// values are rejected at load.
/// </remarks>
public static partial class HardforkLabels
{
    /// <summary>All labels in canonical fork order. Exposed for tests and tooling.</summary>
    /// <remarks>
    /// The contents are produced by <c>Nethermind.Analyzers.HardforkLabelsGenerator</c> from the
    /// <c>Nethermind.Specs.Forks.*</c> <see cref="Nethermind.Specs.Forks.NamedReleaseSpec"/>
    /// subclasses — Forks/*.cs is the single source of truth for the per-fork SIP set.
    /// </remarks>
    public static IReadOnlyList<IHardforkLabel> All { get; } = BuildAll();

    /// <summary>Implemented by the source generator; emits the explicit <c>Block</c>/<c>Time</c> registrations.</summary>
#if ZK_SAVM
    // The source generator isn't wired into the ZisK guest build (the guest builds its spec from an
    // embedded chain_config and never enumerates the label registry), so the generated partial is
    // absent — stub it.
    private static IReadOnlyList<IHardforkLabel> BuildAll() => [];
#else
    private static partial IReadOnlyList<IHardforkLabel> BuildAll();
#endif

    /// <summary>
    /// Expands every hardfork label that <paramref name="source"/> carries — looked up in either
    /// <see cref="IHasNamedForks.NamedForkBlocks"/> or <see cref="IHasNamedForks.NamedForkTimestamps"/>
    /// depending on each label's <see cref="IHardforkLabel.Kind"/> — into the constituent per-SIP
    /// transition fields on <paramref name="target"/>. Throws
    /// <see cref="InvalidConfigurationException"/> when a label disagrees with an explicit per-SIP
    /// value already on the target.
    /// </summary>
    /// <remarks>
    /// Parity-style chainspecs pass the same instance for both parameters (it implements both
    /// interfaces). Geth-style genesis loaders pass the destination <see cref="ChainParameters"/>
    /// as target and the parsed config as source.
    /// </remarks>
    public static void ExpandAll(this ChainParameters target, IHasNamedForks source)
    {
        foreach (IHardforkLabel label in All) label.Apply(target, source);
    }

    /// <param name="name">Fork class name (e.g. <c>Homestead</c>); the JSON wire key is its camelCase form.</param>
    internal static HardforkLabel<ulong> Block(
        string name,
        params Expression<Func<ChainParameters, ulong?>>[] sips) =>
        new(name, HardforkLabelKind.Block,
            (s, k) => s.NamedForkBlocks is { } d && d.TryGetValue(k, out ulong v) ? v : null,
            sips);

    /// <inheritdoc cref="Block"/>
    internal static HardforkLabel<ulong> Time(
        string name,
        params Expression<Func<ChainParameters, ulong?>>[] sips) =>
        new(name, HardforkLabelKind.Timestamp,
            (s, k) => s.NamedForkTimestamps is { } d && d.TryGetValue(k, out ulong v) ? v : null,
            sips);
}

/// <summary>
/// Wire-format object that surfaces hardfork shorthand keys as strongly-typed lookups.
/// <see cref="HardforkLabels.ExpandAll"/> consumes recognized entries to populate the per-SIP
/// transition fields on an <see cref="ChainParameters"/> target.
/// </summary>
public interface IHasNamedForks
{
    /// <summary>Pre-merge fork → activation block number. Case-insensitive lookup.</summary>
    IReadOnlyDictionary<string, ulong>? NamedForkBlocks { get; }

    /// <summary>Post-merge fork → activation timestamp. Case-insensitive lookup.</summary>
    IReadOnlyDictionary<string, ulong>? NamedForkTimestamps { get; }
}

/// <summary>Whether a label fans out to block-number SIPs (<see cref="Block"/>) or timestamp SIPs (<see cref="Timestamp"/>).</summary>
public enum HardforkLabelKind { Block, Timestamp }

public interface IHardforkLabel
{
    /// <summary>Whether this label is read from <see cref="IHasNamedForks.NamedForkBlocks"/> or <see cref="IHasNamedForks.NamedForkTimestamps"/>.</summary>
    HardforkLabelKind Kind { get; }

    /// <summary>Fork class name in PascalCase, e.g. <c>SilaShanghai</c>. The JSON wire key is its camelCase form.</summary>
    string LabelName { get; }

    /// <summary>Property names of every per-SIP transition field this label fans out to.</summary>
    IReadOnlyList<string> SipPropertyNames { get; }

    /// <summary>
    /// Canonical SIP numbers activated (or, for <c>Sip&lt;N&gt;DisableTransition</c>, revoked) by
    /// this label, parsed from <see cref="SipPropertyNames"/> and deduplicated.
    /// </summary>
    /// <remarks>
    /// Parity-split fields (<c>Sip161abcTransition</c>, <c>Sip161dTransition</c>) both fold to
    /// SIP-158 — the canonical SIP number that the runtime <c>IsSip158Enabled</c> flag tracks.
    /// </remarks>
    IReadOnlyList<int> Sips { get; }

    /// <summary>
    /// If <paramref name="source"/> exposes this label's key in the dictionary matching
    /// <see cref="Kind"/>, copies the value into each constituent SIP field on
    /// <paramref name="target"/> that is still unset. Throws
    /// <see cref="InvalidConfigurationException"/> when an SIP field is already set to a
    /// different value.
    /// </summary>
    void Apply(ChainParameters target, IHasNamedForks source);
}

internal sealed class HardforkLabel<T> : IHardforkLabel
    where T : struct, IEquatable<T>, IFormattable
{
    // Sip{N}[abc|d][Disable]Transition[Timestamp] — captures the SIP number and the Parity-split
    // marker so SipPropertyNames can be normalized to canonical SIPs in `Sips`.
    private static readonly Regex SipPropertyPattern =
        new(@"^Sip(?<sip>\d+)(?<split>abc|d)?(?:Disable)?Transition(?:Timestamp)?$", RegexOptions.Compiled);

    private readonly Func<IHasNamedForks, string, T?> _readValue;
    private readonly SipAccessor[] _sips;

    public HardforkLabelKind Kind { get; }
    public string LabelName { get; }
    public IReadOnlyList<string> SipPropertyNames { get; }
    public IReadOnlyList<int> Sips { get; }

    public HardforkLabel(
        string labelName,
        HardforkLabelKind kind,
        Func<IHasNamedForks, string, T?> readValue,
        Expression<Func<ChainParameters, T?>>[] sips)
    {
        LabelName = labelName;
        Kind = kind;
        _readValue = readValue;
        _sips = [.. sips.Select(static e => new SipAccessor(e.GetName(), e.Compile(), e.GetSetter()))];
        SipPropertyNames = [.. _sips.Select(static e => e.Name)];
        Sips = [.. SipPropertyNames.Select(ParseCanonicalSip).Distinct()];
    }

    public void Apply(ChainParameters target, IHasNamedForks source)
    {
        T? labelValue = _readValue(source, LabelName);
        if (labelValue is null) return;

        foreach (SipAccessor sip in _sips)
        {
            T? current = sip.Read(target);
            if (current is null)
            {
                sip.Write(target, labelValue.Value);
            }
            else if (!current.Value.Equals(labelValue.Value))
            {
                throw new InvalidConfigurationException(
                    $"Chainspec hardfork label '{LabelName}' = 0x{labelValue.Value.ToString("x", null)} conflicts with explicit {sip.Name} = 0x{current.Value.ToString("x", null)}. Either remove the conflicting field or align both values.",
                    ExitCodes.ConflictingChainspecSipConfiguration);
            }
        }
    }

    private static int ParseCanonicalSip(string propertyName)
    {
        Match m = SipPropertyPattern.Match(propertyName);
        if (!m.Success) throw new ArgumentException(
            $"Property name '{propertyName}' does not follow the Sip<N>[abc|d][Disable]Transition[Timestamp] convention.",
            nameof(propertyName));
        // Parity chainspecs split SIP-158 (state-clearing) into Sip161abc + Sip161d; both map to SIP-158.
        return m.Groups["split"].Success ? 158 : int.Parse(m.Groups["sip"].Value);
    }

    private readonly record struct SipAccessor(string Name, Func<ChainParameters, T?> Read, Action<ChainParameters, T?> Write);
}
