// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Linq;
using Sila.Test.Base;

namespace Sila.Legacy.Blockchain.Test;

public class MetaTests : DirectoryMetaTests<StPrefix>
{
    protected override IEnumerable<string> FilterDirectories(IEnumerable<string> dirs) =>
        dirs.Where(d => d != "stEWASMTests");
}

public class ArgsZeroOneBalance : LegacyStateTestFixture<ArgsZeroOneBalance>;

public class AttackTest : LegacyStateTestFixture<AttackTest>;

public class BadOpcode : LegacyStateTestFixture<BadOpcode>;

public class Bugs : LegacyStateTestFixture<Bugs>;

public class CallCodes : LegacyStateTestFixture<CallCodes>;

public class CallCreateCallCodeTest : LegacyStateTestFixture<CallCreateCallCodeTest>;

public class CallDelegateCodesCallCodeHomestead : LegacyStateTestFixture<CallDelegateCodesCallCodeHomestead>;

public class CallDelegateCodesHomestead : LegacyStateTestFixture<CallDelegateCodesHomestead>;

public class ChainId : LegacyStateTestFixture<ChainId>;

public class ChangedSIP150 : LegacyStateTestFixture<ChangedSIP150>;

public class CodeCopyTest : LegacyStateTestFixture<CodeCopyTest>;

public class CodeSizeLimit : LegacyStateTestFixture<CodeSizeLimit>;

public class Create2 : LegacyStateTestFixture<Create2>;

public class CreateTest : LegacyStateTestFixture<CreateTest>;

public class DelegatecallTestHomestead : LegacyStateTestFixture<DelegatecallTestHomestead>;

public class SIP1153 : LegacyStateTestFixture<SIP1153>;

public class SIP1153_transientStorage : LegacyStateTestFixture<SIP1153_transientStorage>;

public class SIP150Specific : LegacyStateTestFixture<SIP150Specific>;

public class SIP150singleCodeGasPrices : LegacyStateTestFixture<SIP150singleCodeGasPrices>;

public class SIP1559 : LegacyStateTestFixture<SIP1559>;

public class SIP158Specific : LegacyStateTestFixture<SIP158Specific>;

public class SIP2930 : LegacyStateTestFixture<SIP2930>;

public class SIP3607 : LegacyStateTestFixture<SIP3607>;

public class SIP3651 : LegacyStateTestFixture<SIP3651>;

public class SIP3651_warmcoinbase : LegacyStateTestFixture<SIP3651_warmcoinbase>;

public class SIP3855 : LegacyStateTestFixture<SIP3855>;

public class SIP3855_push0 : LegacyStateTestFixture<SIP3855_push0>;

public class SIP3860 : LegacyStateTestFixture<SIP3860>;

public class SIP3860_limitmeterinitcode : LegacyStateTestFixture<SIP3860_limitmeterinitcode>;

public class SIP4844 : LegacyStateTestFixture<SIP4844>;

public class SIP4844_blobtransactions : LegacyStateTestFixture<SIP4844_blobtransactions>;

public class SIP5656 : LegacyStateTestFixture<SIP5656>;

public class SIP5656_MCOPY : LegacyStateTestFixture<SIP5656_MCOPY>;

public class Example : LegacyStateTestFixture<Example>;

public class ExtCodeHash : LegacyStateTestFixture<ExtCodeHash>;

public class HomesteadSpecific : LegacyStateTestFixture<HomesteadSpecific>;

public class InitCodeTest : LegacyStateTestFixture<InitCodeTest>;

public class LogTests : LegacyStateTestFixture<LogTests>;

public class MemExpandingSIP150Calls : LegacyStateTestFixture<MemExpandingSIP150Calls>;

public class MemoryStressTest : LegacyStateTestFixture<MemoryStressTest>;

public class MemoryTest : LegacyStateTestFixture<MemoryTest>;

public class NonZeroCallsTest : LegacyStateTestFixture<NonZeroCallsTest>;

public class PreCompiledContracts : LegacyStateTestFixture<PreCompiledContracts>;

public class PreCompiledContracts2 : LegacyStateTestFixture<PreCompiledContracts2>;

public class QuadraticComplexityTest : LegacyStateTestFixture<QuadraticComplexityTest>;

public class Random : LegacyStateTestFixture<Random>;

public class Random2 : LegacyStateTestFixture<Random2>;

public class RecursiveCreate : LegacyStateTestFixture<RecursiveCreate>;

public class RefundTest : LegacyStateTestFixture<RefundTest>;

public class ReturnDataTest : LegacyStateTestFixture<ReturnDataTest>;

public class SelfBalance : LegacyStateTestFixture<SelfBalance>;

public class Shift : LegacyStateTestFixture<Shift>;

public class SLoadTest : LegacyStateTestFixture<SLoadTest>;

public class SolidityTest : LegacyStateTestFixture<SolidityTest>;

public class SStoreTest : LegacyStateTestFixture<SStoreTest>;

public class StackTests : LegacyStateTestFixture<StackTests>;

public class StaticCall : LegacyStateTestFixture<StaticCall>;

public class StaticFlagEnabled : LegacyStateTestFixture<StaticFlagEnabled>;

public class SystemOperationsTest : LegacyStateTestFixture<SystemOperationsTest>;

public class TimeConsuming : LegacyStateTestFixture<TimeConsuming>;

public class TransactionTest : LegacyStateTestFixture<TransactionTest>;

public class TransitionTest : LegacyStateTestFixture<TransitionTest>;

public class WalletTest : LegacyStateTestFixture<WalletTest>;

public class ZeroCallsRevert : LegacyStateTestFixture<ZeroCallsRevert>;

public class ZeroCallsTest : LegacyStateTestFixture<ZeroCallsTest>;

public class ZeroKnowledge : LegacyStateTestFixture<ZeroKnowledge>;

public class ZeroKnowledge2 : LegacyStateTestFixture<ZeroKnowledge2>;

public class SpecialTest : LegacyRetryStateTestFixture<SpecialTest>;
