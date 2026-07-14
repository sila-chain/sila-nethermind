// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;

namespace Sila.Test.Base
{
    public interface IStateTestRunner
    {
        IEnumerable<SilaTestResult> RunTests();
    }
}
