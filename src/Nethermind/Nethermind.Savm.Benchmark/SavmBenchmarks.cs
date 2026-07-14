// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using BenchmarkDotNet.Attributes;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test;
using Nethermind.Savm.CodeAnalysis;
using Nethermind.Specs;
using Nethermind.Savm.GasPolicy;
using Nethermind.Savm.State;
using Nethermind.Savm.Tracing;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Blockchain;

namespace Nethermind.Savm.Benchmark
{
    [MemoryDiagnoser]
    public class SavmBenchmarks
    {
        public static byte[] ByteCode { get; set; }

        private IReleaseSpec _spec = MainnetSpecProvider.Instance.GetSpec((ForkActivation)MainnetSpecProvider.IstanbulBlockNumber);
        private ITxTracer _txTracer = NullTxTracer.Instance;
        private ExecutionEnvironment _environment;
        private IVirtualMachine _virtualMachine;
        private BlockHeader _header = new(Keccak.Zero, Keccak.Zero, Address.Zero, UInt256.One, MainnetSpecProvider.IstanbulBlockNumber, Int64.MaxValue, 1UL, Bytes.Empty);
        private IBlockhashProvider _blockhashProvider = new TestBlockhashProvider();
        private VmState<SilaGasPolicy> _savmState;
        private IWorldState _stateProvider;

        [GlobalSetup]
        public void GlobalSetup()
        {
            ByteCode = Bytes.FromHexString(Environment.GetEnvironmentVariable("NETH.BENCHMARK.BYTECODE") ?? string.Empty);
            Console.WriteLine($"Running benchmark for bytecode {ByteCode?.ToHexString()}");

            _stateProvider = TestWorldStateFactory.CreateForTest();
            _stateProvider.CreateAccount(Address.Zero, 1000.Sila);
            _stateProvider.Commit(_spec);
            SilaCodeInfoRepository codeInfoRepository = new(_stateProvider);
            _virtualMachine = new SilaVirtualMachine(_blockhashProvider, MainnetSpecProvider.Instance, LimboLogs.Instance);
            _virtualMachine.SetBlockExecutionContext(new BlockExecutionContext(_header, _spec));
            _virtualMachine.SetTxExecutionContext(new TxExecutionContext(Address.Zero, codeInfoRepository, null, 0));

            _environment = ExecutionEnvironment.Rent(
                executingAccount: Address.Zero,
                codeSource: Address.Zero,
                caller: Address.Zero,
                codeInfo: new CodeInfo(ByteCode),
                callDepth: 0,
                value: 0,
                inputData: default
            );

            _savmState = VmState<SilaGasPolicy>.RentTopLevel(SilaGasPolicy.FromULong(ulong.MaxValue), ExecutionType.TRANSACTION, _environment, new StackAccessTracker(), _stateProvider.TakeSnapshot());
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _savmState.Dispose();
            _environment.Dispose();
        }

        [Benchmark]
        public void ExecuteCode()
        {
            _virtualMachine.ExecuteTransaction<OffFlag>(_savmState, _stateProvider, _txTracer);
            _stateProvider.Reset();
        }
    }
}
