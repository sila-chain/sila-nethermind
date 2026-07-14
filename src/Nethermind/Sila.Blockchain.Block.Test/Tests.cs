// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sila.Test.Base;

namespace Sila.Blockchain.Block.Test;

public class MetaTests : DirectoryMetaTests<BcPrefix>
{
    protected override IEnumerable<string> FilterDirectories(IEnumerable<string> dirs) =>
        dirs.Where(d => !new DirectoryInfo(Path.Combine(GetTestsDirectory(), d))
            .GetFiles().Any(f => f.Name.Contains(".resources.")));
}

public class BlockGasLimitTest : BlockchainTestFixture<BlockGasLimitTest>;

public class SIP1153_transientStorage : BlockchainTestFixture<SIP1153_transientStorage>;

public class SIP1559 : BlockchainTestFixture<SIP1559>;

public class SIP3675 : BlockchainTestFixture<SIP3675>;

public class SIP4844_blobtransactions : BlockchainTestFixture<SIP4844_blobtransactions>;

public class _4895_withdrawals : BlockchainTestFixture<_4895_withdrawals>;

public class SIP6780 : BlockchainTestFixture<SIP6780>;

public class Example : BlockchainTestFixture<Example>;

public class ExploitTest : BlockchainTestFixture<ExploitTest>;

public class ForgedTest : BlockchainTestFixture<ForgedTest>;

public class ForkStressTest : BlockchainTestFixture<ForkStressTest>;

public class GasPricerTest : BlockchainTestFixture<GasPricerTest>;

public class InvalidHeaderTest : BlockchainTestFixture<InvalidHeaderTest>;

public class MultiChainTest : BlockchainTestFixture<MultiChainTest>;

public class RandomBlockhashTest : BlockchainTestFixture<RandomBlockhashTest>;

public class StateTests : BlockchainTestFixture<StateTests>;

public class TotalDifficultyTest : BlockchainTestFixture<TotalDifficultyTest>;

public class UncleHeaderValidity : BlockchainTestFixture<UncleHeaderValidity>;

public class UncleSpecialTests : BlockchainTestFixture<UncleSpecialTests>;

public class UncleTest : BlockchainTestFixture<UncleTest>;

public class ValidBlockTest : BlockchainTestFixture<ValidBlockTest>;

public class WalletTest : BlockchainTestFixture<WalletTest>;
