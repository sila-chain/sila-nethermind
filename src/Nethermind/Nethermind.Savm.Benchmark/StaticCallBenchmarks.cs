// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
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
using Nethermind.Specs.Forks;
using Nethermind.Blockchain;

namespace Nethermind.Savm.Benchmark
{
    [MemoryDiagnoser]
    public class StaticCallBenchmarks
    {
        private IReleaseSpec _spec = MainnetSpecProvider.Instance.GetSpec((ForkActivation)MainnetSpecProvider.IstanbulBlockNumber);
        private ITxTracer _txTracer = NullTxTracer.Instance;
        private ExecutionEnvironment _environment;
        private IVirtualMachine _virtualMachine;
        private BlockHeader _header = new(Keccak.Zero, Keccak.Zero, Address.Zero, UInt256.One, MainnetSpecProvider.MuirGlacierBlockNumber, Int64.MaxValue, 1UL, Bytes.Empty);
        private IBlockhashProvider _blockhashProvider = new TestBlockhashProvider();
        private VmState<SilaGasPolicy> _savmState;
        private IWorldState _stateProvider;

        public IEnumerable<byte[]> Bytecodes
        {
            get
            {
                yield return bytecode1;
                yield return bytecode2;
            }
        }

        byte[] bytecode1 = Prepare.SavmCode
            .Op(Instruction.JUMPDEST)
            .PushData(0)
            .Op(Instruction.DUP1)
            .Op(Instruction.DUP1)
            .Op(Instruction.DUP1)
            .PushData(4)
            .Op(Instruction.GAS)
            .Op(Instruction.STATICCALL)
            .Op(Instruction.POP)
            .PushData(0)
            .Op(Instruction.JUMP)
            .Done;

        byte[] bytecode2 = Prepare.SavmCode
            .Op(Instruction.JUMPDEST)
            .PushData(0)
            .Op(Instruction.DUP1)
            .Op(Instruction.DUP1)
            .Op(Instruction.DUP1)
            .PushData(4)
            .Op(Instruction.GAS)
            .Op(Instruction.POP)
            .Op(Instruction.POP)
            .Op(Instruction.POP)
            .Op(Instruction.POP)
            .Op(Instruction.POP)
            .Op(Instruction.POP)
            .PushData(0)
            .Op(Instruction.JUMP)
            .Done;

        [ParamsSource(nameof(Bytecodes))]
        public byte[] Bytecode { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _stateProvider = TestWorldStateFactory.CreateForTest();
            _stateProvider.CreateAccount(Address.Zero, 1000.Sila);
            _stateProvider.Commit(_spec);

            Console.WriteLine(MuirGlacier.Instance);
            SilaCodeInfoRepository codeInfoRepository = new(_stateProvider);
            _virtualMachine = new SilaVirtualMachine(_blockhashProvider, MainnetSpecProvider.Instance, new OneLoggerLogManager(NullLogger.Instance));
            _virtualMachine.SetBlockExecutionContext(new BlockExecutionContext(_header, _spec));
            _virtualMachine.SetTxExecutionContext(new TxExecutionContext(Address.Zero, codeInfoRepository, null, 0));
            _environment = ExecutionEnvironment.Rent(
                executingAccount: Address.Zero,
                codeSource: Address.Zero,
                caller: Address.Zero,
                codeInfo: new CodeInfo(Bytecode),
                callDepth: 0,
                value: 0,
                inputData: default
            );

            _savmState = VmState<SilaGasPolicy>.RentTopLevel(SilaGasPolicy.FromULong(100_000_000UL), ExecutionType.TRANSACTION, _environment, new StackAccessTracker(), _stateProvider.TakeSnapshot());
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _savmState.Dispose();
            _environment.Dispose();
        }

        [Benchmark(Baseline = true)]
        public void ExecuteCode()
        {
            _virtualMachine.ExecuteTransaction<OffFlag>(_savmState, _stateProvider, _txTracer);
            _stateProvider.Reset();
        }

        [Benchmark]
        public void ExecuteCodeNoTracing()
        {
            _virtualMachine.ExecuteTransaction<OffFlag>(_savmState, _stateProvider, _txTracer);
            _stateProvider.Reset();
        }

        [Benchmark]
        public void No_machine_running() => _stateProvider.Reset();
    }
}
