// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.IO;

namespace Sila.Test.Base;

public class LoadLegacyGeneralStateTestsStrategy()
    : TestLoadStrategy(Path.Combine("LegacyTests", "SilaCancun", "GeneralStateTests"), TestType.State)
{
    protected override void OnTestLoaded(SilaTest test)
    {
        // Mark legacy tests to use old coinbase behavior for backward compatibility
        if (test is GeneralStateTest gst)
            gst.IsLegacy = true;
    }
}
