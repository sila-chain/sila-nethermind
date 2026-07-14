// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Blockchain.Find;

namespace Nethermind.JsonRpc.Modules.Sil.FeeHistory
{
    public interface IFeeHistoryOracle
    {
        ResultWrapper<FeeHistoryResults> GetFeeHistory(ulong blockCount, BlockParameter newestBlock, double[] rewardPercentiles);
    }
}
