// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Linq;
using Nethermind.Savm.Tracing;
using Nethermind.Int256;
using NUnit.Framework;

namespace Nethermind.Savm.Test;

[TestFixture]
public abstract class SavmMemoryTestsBase
{
    [Test]
    public void Save_empty_beyond_reasonable_size_does_not_throw()
    {
        SavmPooledMemory memory = new();
        UInt256 dest = (UInt256)int.MaxValue + 1;
        bool outOfGas = !memory.TrySave(in dest, Array.Empty<byte>());
        Assert.That(outOfGas, Is.EqualTo(false));
    }

    [Test]
    public void Trace_one_word()
    {
        SavmPooledMemory memory = new();
        UInt256 dest = UInt256.Zero;
        bool outOfGas = !memory.TrySaveWord(in dest, new byte[SavmPooledMemory.WordSize]);
        Assert.That(outOfGas, Is.EqualTo(false));
        TraceMemory trace = memory.GetTrace();
        Assert.That(trace.ToHexWordList().Count, Is.EqualTo(1));
    }

    [Test]
    public void Trace_two_words()
    {
        SavmPooledMemory memory = new();
        UInt256 dest = SavmPooledMemory.WordSize;
        bool outOfGas = !memory.TrySaveWord(in dest, new byte[SavmPooledMemory.WordSize]);
        Assert.That(outOfGas, Is.EqualTo(false));
        TraceMemory trace = memory.GetTrace();
        Assert.That(trace.ToHexWordList().Count, Is.EqualTo(2));
    }

    [Test]
    public void Trace_overwrite()
    {
        SavmPooledMemory memory = new();
        UInt256 dest = SavmPooledMemory.WordSize;
        bool outOfGas = !memory.TrySaveWord(in dest, new byte[SavmPooledMemory.WordSize]);
        Assert.That(outOfGas, Is.EqualTo(false));
        outOfGas = !memory.TrySaveWord(in dest, new byte[SavmPooledMemory.WordSize]);
        Assert.That(outOfGas, Is.EqualTo(false));
        TraceMemory trace = memory.GetTrace();
        Assert.That(trace.ToHexWordList().Count, Is.EqualTo(2));
    }

    [Test]
    public void Trace_when_position_not_on_word_border()
    {
        SavmPooledMemory memory = new();
        UInt256 dest = SavmPooledMemory.WordSize / 2;
        bool outOfGas = !memory.TrySaveByte(in dest, 1);
        Assert.That(outOfGas, Is.EqualTo(false));
        TraceMemory trace = memory.GetTrace();
        Assert.That(trace.ToHexWordList().Count, Is.EqualTo(1));
    }

    [Test]
    public void Calculate_memory_cost_returns_0_for_subsequent_calls()
    {
        SavmPooledMemory memory = new();
        UInt256 dest = UInt256.One;
        memory.CalculateMemoryCost(in dest, UInt256.One, out bool outOfGas);
        Assert.That(outOfGas, Is.EqualTo(false));
        ulong cost = memory.CalculateMemoryCost(in dest, UInt256.One, out outOfGas);
        Assert.That(outOfGas, Is.EqualTo(false));
        Assert.That(cost, Is.EqualTo(0UL));
    }

    [Test]
    public void Calculate_memory_cost_returns_0_for_0_length()
    {
        SavmPooledMemory memory = new();
        UInt256 dest = long.MaxValue;
        ulong cost = memory.CalculateMemoryCost(in dest, UInt256.Zero, out bool outOfGas);
        Assert.That(outOfGas, Is.EqualTo(false));
        Assert.That(cost, Is.EqualTo(0UL));
    }
}
