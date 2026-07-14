// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Sila.Blockchain.Pyspec.Test.ZkSavmFixtures;

// zkSAVM fixtures moved to sila/execution-specs after v0.4.0; the loader's default
// ARCHIVE_URL_TEMPLATE already points there, so only the version/name need overriding.
public static class Constants
{
    public const string ArchiveVersion = "tests-zksavm@v0.5.0";
    public const string ArchiveName = "fixtures_zksavm.tar.gz";
}
