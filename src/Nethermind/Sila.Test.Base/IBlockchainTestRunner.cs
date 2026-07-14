// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sila.Test.Base
{
    public interface IBlockchainTestRunner
    {
        Task<IEnumerable<SilaTestResult>> RunTestsAsync();
    }
}
