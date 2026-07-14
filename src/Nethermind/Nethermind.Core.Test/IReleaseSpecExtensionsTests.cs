// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core.Specs;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Core.Test;

public class IReleaseSpecExtensionsTests
{
    [Test]
    public void WithoutSip158_returns_the_same_cached_wrapper_per_spec() => AssertWrapperIsCachedPerSpec(
        enable: static (spec, value) => spec.IsSip158Enabled.Returns(value),
        wrap: static spec => spec.WithoutSip158(),
        isEnabled: static spec => spec.IsSip158Enabled);

    [Test]
    public void WithoutSip3607_returns_the_same_cached_wrapper_per_spec() => AssertWrapperIsCachedPerSpec(
        enable: static (spec, value) => spec.IsSip3607Enabled.Returns(value),
        wrap: static spec => spec.WithoutSip3607(),
        isEnabled: static spec => spec.IsSip3607Enabled);

    private static void AssertWrapperIsCachedPerSpec(
        Action<IReleaseSpec, bool> enable,
        Func<IReleaseSpec, IReleaseSpec> wrap,
        Func<IReleaseSpec, bool> isEnabled)
    {
        IReleaseSpec disabledSpec = ReleaseSpecSubstitute.Create();
        enable(disabledSpec, false);
        Assert.That(wrap(disabledSpec), Is.SameAs(disabledSpec));

        IReleaseSpec spec = ReleaseSpecSubstitute.Create();
        enable(spec, true);
        IReleaseSpec wrapper = wrap(spec);
        Assert.That(wrapper, Is.Not.SameAs(spec));
        Assert.That(isEnabled(wrapper), Is.False);
        Assert.That(wrap(spec), Is.SameAs(wrapper));
        Assert.That(wrap(wrapper), Is.SameAs(wrapper));

        IReleaseSpec otherSpec = ReleaseSpecSubstitute.Create();
        enable(otherSpec, true);
        Assert.That(wrap(otherSpec), Is.Not.SameAs(wrapper));

        enable(spec, false);
        Assert.That(wrap(spec), Is.SameAs(spec));
    }
}
