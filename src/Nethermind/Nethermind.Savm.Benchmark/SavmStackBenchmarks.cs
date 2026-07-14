// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Nethermind.Core;
using Nethermind.Savm.Tracing;
using Nethermind.Int256;

namespace Nethermind.Savm.Benchmark
{
    public class SavmStackBenchmarks
    {
        public IEnumerable<UInt256> ValueSource => new[]
        {
            UInt256.Parse("125124123718263172357123"),
            UInt256.Parse("0"),
            UInt256.MaxValue
        };

        private byte[] _stack;

        [GlobalSetup]
        public void GlobalSetup() => _stack = new byte[(SavmStack.MaxStackSize + SavmStack.RegisterLength * 32) * 1024];

        [Benchmark(OperationsPerInvoke = 4)]
        [ArgumentsSource(nameof(ValueSource))]
        public UInt256 Uint256(UInt256 v)
        {
            SavmStack stack = new(0, NullTxTracer.Instance, ref MemoryMarshal.GetArrayDataReference(_stack), default);

            stack.PushUInt256<OffFlag>(in v);
            stack.PopUInt256(out UInt256 value);

            stack.PushUInt256<OffFlag>(in value);
            stack.PopUInt256(out value);

            stack.PushUInt256<OffFlag>(in value);
            stack.PopUInt256(out value);

            stack.PushUInt256<OffFlag>(in value);
            stack.PopUInt256(out value);

            return value;
        }

        [Benchmark(OperationsPerInvoke = 4)]
        public byte Byte()
        {
            SavmStack stack = new(0, NullTxTracer.Instance, ref MemoryMarshal.GetArrayDataReference(_stack), default);

            int b = 1;

            stack.PushByte<OffFlag>((byte)b);
            b = stack.PopByte();

            stack.PushByte<OffFlag>((byte)b);
            b = stack.PopByte();

            stack.PushByte<OffFlag>((byte)b);
            b = stack.PopByte();

            stack.PushByte<OffFlag>((byte)b);
            b = stack.PopByte();

            return (byte)b;
        }

        [Benchmark(OperationsPerInvoke = 4)]
        public void PushZero()
        {
            SavmStack stack = new(0, NullTxTracer.Instance, ref MemoryMarshal.GetArrayDataReference(_stack), default);

            stack.PushZero<OffFlag>();
            stack.PushZero<OffFlag>();
            stack.PushZero<OffFlag>();
            stack.PushZero<OffFlag>();
        }

        [Benchmark(OperationsPerInvoke = 4)]
        public void PushOne()
        {
            SavmStack stack = new(0, NullTxTracer.Instance, ref MemoryMarshal.GetArrayDataReference(_stack), default);

            stack.PushOne<OffFlag>();
            stack.PushOne<OffFlag>();
            stack.PushOne<OffFlag>();
            stack.PushOne<OffFlag>();
        }

        [Benchmark(OperationsPerInvoke = 4)]
        public void Swap()
        {
            SavmStack stack = new(0, NullTxTracer.Instance, ref MemoryMarshal.GetArrayDataReference(_stack), default);

            stack.Swap<OffFlag>(2);
            stack.Swap<OffFlag>(2);
            stack.Swap<OffFlag>(2);
            stack.Swap<OffFlag>(2);
        }

        [Benchmark(OperationsPerInvoke = 4)]
        public void Dup()
        {
            SavmStack stack = new(1, NullTxTracer.Instance, ref MemoryMarshal.GetArrayDataReference(_stack), default);

            stack.Dup<OffFlag>(1);
            stack.Dup<OffFlag>(1);
            stack.Dup<OffFlag>(1);
            stack.Dup<OffFlag>(1);
        }
    }
}
