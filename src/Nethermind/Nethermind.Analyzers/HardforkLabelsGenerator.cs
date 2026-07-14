// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nethermind.Analyzers;

/// <summary>
/// Emits the implementation of <c>Nethermind.Specs.ChainSpecStyle.HardforkLabels.BuildAll</c>
/// from the <c>Nethermind.Specs.Forks.*</c> <c>NamedReleaseSpec</c> subclasses, making Forks/*.cs
/// the single source of truth for per-fork SIP membership.
/// </summary>
/// <remarks>
/// For every concrete subclass of <c>NamedReleaseSpec&lt;TSelf&gt;</c> the generator scans the
/// <c>Apply</c> method body for <c>spec.IsSip{N}Enabled = literal;</c> assignments, extracts the
/// parent fork from the primary-constructor base argument
/// (<c>NamedReleaseSpec&lt;SilaCancun&gt;(SilaShanghai.Instance)</c> → parent <c>SilaShanghai</c>), and emits
/// a <c>Block</c> or <c>Time</c> registration.
/// </remarks>
[Generator]
public sealed class HardforkLabelsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ForkInfo?> forks = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, _) => TryExtractFork(ctx))
            .Where(static f => f is not null);

        context.RegisterSourceOutput(forks.Collect(), static (spc, collected) =>
        {
            ImmutableArray<ForkInfo> all = [.. collected.Where(f => f is not null).Select(f => f!)];
            if (all.IsEmpty) return;

            // Apply only records this fork's own delta — walk parents for cumulative IsPostMerge and Depth.
            Dictionary<string, ForkInfo> byName = all.ToDictionary(f => f.Name);
            foreach (ForkInfo fork in all)
            {
                int depth = 0;
                for (ForkInfo? c = fork; c is not null; c = c.ParentName is { } pn && byName.TryGetValue(pn, out ForkInfo p) ? p : null)
                {
                    if (c.SetsIsPostMerge) fork.IsPostMerge = true;
                    if (c != fork) depth++;
                }
                fork.Depth = depth;
            }

            spc.AddSource("HardforkLabels.g.cs", SourceText.From(Emit(all), Encoding.UTF8));
        });
    }

    private static ForkInfo? TryExtractFork(GeneratorSyntaxContext ctx)
    {
        ClassDeclarationSyntax cls = (ClassDeclarationSyntax)ctx.Node;
        // Immediate base must be NamedReleaseSpec<TSelf> (not NamedGnosisReleaseSpec or any other
        // intermediate). Only sila-mainnet forks contribute to the public HardforkLabels list.
        if (ctx.SemanticModel.GetDeclaredSymbol(cls) is not INamedTypeSymbol { IsAbstract: false, BaseType: { Name: "NamedReleaseSpec" } } symbol) return null;
        if (AnalyzeApplyMethod(cls) is not { } analysis) return null;

        return new ForkInfo
        {
            Name = symbol.Name,
            ParentName = ExtractParentName(cls),
            SipDelta = analysis.SipDelta,
            SetsIsPostMerge = analysis.SetsIsPostMerge,
        };
    }

    /// <summary>
    /// Parse the parent fork from <c>: NamedReleaseSpec&lt;TSelf&gt;(SilaShanghai.Instance)</c> — the
    /// argument to the primary-constructor base list is a <c>MemberAccessExpression</c> of the form
    /// <c>&lt;ForkName&gt;.Instance</c>, or <c>null</c> for the root fork (Olympic).
    /// </summary>
    private static string? ExtractParentName(ClassDeclarationSyntax cls) =>
        cls.BaseList?.Types
            .OfType<PrimaryConstructorBaseTypeSyntax>()
            .Select(b => b.ArgumentList.Arguments.FirstOrDefault()?.Expression)
            .OfType<MemberAccessExpressionSyntax>()
            .Select(m => (m.Expression as IdentifierNameSyntax)?.Identifier.ValueText)
            .FirstOrDefault();

    private sealed class ApplyAnalysis
    {
        public List<(int Sip, bool Enabled)> SipDelta { get; set; } = [];
        public bool SetsIsPostMerge { get; set; }
    }

    private static ApplyAnalysis? AnalyzeApplyMethod(ClassDeclarationSyntax cls)
    {
        MethodDeclarationSyntax? apply = cls.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == "Apply" && m.Modifiers.Any(SyntaxKind.OverrideKeyword));
        if (apply is null) return null;

        List<(int, bool)> delta = [];
        bool setsIsPostMerge = false;
        foreach (AssignmentExpressionSyntax asn in apply.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            // Match `spec.<Property> = <bool literal>`; anything else is consensus-engine or non-SIP setup.
            if (asn.Left is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "spec" } } ma) continue;
            if (asn.Right is not LiteralExpressionSyntax rhs) continue;

            string propName = ma.Name.Identifier.ValueText;
            if (propName == "IsPostMerge")
            {
                if (rhs.Token.IsKind(SyntaxKind.TrueKeyword)) setsIsPostMerge = true;
                continue;
            }
            if (!propName.StartsWith("IsSip", StringComparison.Ordinal) || !propName.EndsWith("Enabled", StringComparison.Ordinal)) continue;
            string middle = propName.Substring("IsSip".Length, propName.Length - "IsSip".Length - "Enabled".Length);
            if (!int.TryParse(middle, out int sip)) continue;

            if (rhs.Token.IsKind(SyntaxKind.TrueKeyword)) delta.Add((sip, true));
            else if (rhs.Token.IsKind(SyntaxKind.FalseKeyword)) delta.Add((sip, false));
        }
        return new ApplyAnalysis { SipDelta = delta, SetsIsPostMerge = setsIsPostMerge };
    }

    private static string Emit(ImmutableArray<ForkInfo> forks)
    {
        StringBuilder sb = new();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by Nethermind.Analyzers.HardforkLabelsGenerator.");
        sb.AppendLine("// Edits should target Forks/*.cs (the SIP delta) or the generator (JSON-field rules).");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("namespace Nethermind.Specs.ChainSpecStyle;");
        sb.AppendLine();
        sb.AppendLine("partial class HardforkLabels");
        sb.AppendLine("{");
        sb.AppendLine("    private static partial IReadOnlyList<IHardforkLabel> BuildAll() =>");
        sb.AppendLine("    [");

        // Root forks (no parent) and forks whose SIP set has no JSON-visible fields (Olympic /
        // glaciers / SilaParis / BPO*) are silently skipped — they contribute to the runtime SIP
        // state via the Apply chain but aren't user-facing chainspec shortcuts.
        //
        // Emit `nameof(global::Nethermind.Specs.Forks.SilaCancun)` rather than a raw string literal —
        // pins the label name to the actual fork type at compile time, so renaming or removing
        // Forks/SilaCancun.cs surfaces as a compile error on the generated file.
        // Depth-ordered emit so the generated list reads in fork progression.
        foreach (ForkInfo fork in forks
            .OrderBy(f => f.Depth)
            .ThenBy(f => f.Name, StringComparer.Ordinal))
        {
            if (fork.ParentName is null) continue;
            List<string> jsonFields = ResolveJsonFields(fork);
            if (jsonFields.Count == 0) continue;

            string factory = fork.IsPostMerge ? "Time" : "Block";
            sb.Append("        ").Append(factory).Append("(nameof(global::Nethermind.Specs.Forks.").Append(fork.Name).AppendLine("),");
            for (int i = 0; i < jsonFields.Count; i++)
                sb.Append("            p => p.").Append(jsonFields[i]).AppendLine(i == jsonFields.Count - 1 ? ")," : ",");
        }

        sb.AppendLine("    ];");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// SIPs whose JSON representation doesn't follow the conventional
    /// <c>Sip&lt;N&gt;Transition[Timestamp]</c> on <c>ChainSpecParamsJson</c>:
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>No JSON transition field at all (block-reward, difficulty bomb, Byzantium precompile
    ///         activations, SIP-170 MaxCodeSize, Homestead SIP-2 difficulty curve) — dropped from
    ///         the label silently.</item>
    ///   <item>SIP-158 is Parity-split into <c>Sip161abcTransition</c> + <c>Sip161dTransition</c>.</item>
    ///   <item>Disables (<c>spec.IsSipNEnabled = false</c>) map to <c>Sip&lt;N&gt;DisableTransition</c>
    ///         — only ConstantinopleFix does this.</item>
    /// </list>
    /// </remarks>
    private static readonly HashSet<int> NoJsonField =
    [
        2,                   // Homestead — no Sip2Transition
        100, 196, 197, 198,  // Byzantium consensus / precompile activations
        170,                 // MaxCodeSize-tracked
        649, 1234,           // Block-reward / difficulty-bomb deltas
    ];

    private static List<string> ResolveJsonFields(ForkInfo fork)
    {
        List<string> result = [];
        string suffix = fork.IsPostMerge ? "TransitionTimestamp" : "Transition";
        foreach ((int sip, bool enabled) in fork.SipDelta)
        {
            if (!enabled) { result.Add($"Sip{sip}DisableTransition"); continue; }
            if (NoJsonField.Contains(sip)) continue;
            if (sip == 158) { result.Add("Sip161abcTransition"); result.Add("Sip161dTransition"); continue; }
            result.Add($"Sip{sip}{suffix}");
        }
        return result;
    }
}

internal sealed class ForkInfo
{
    public string Name { get; set; } = "";
    public string? ParentName { get; set; }
    public List<(int Sip, bool Enabled)> SipDelta { get; set; } = [];
    public bool SetsIsPostMerge { get; set; }
    public bool IsPostMerge { get; set; }
    public int Depth { get; set; }
}
