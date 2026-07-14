// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Savm.State;
using Nethermind.Savm.TransactionProcessing;
using Nethermind.Int256;

namespace Nethermind.Xdc.Contracts;

public interface IMasternodeVotingContract
{
    Address[] GetCandidatesByStake(BlockHeader blockHeader);
    Address[] GetCandidates(BlockHeader blockHeader);
    Address[] GetCandidates(ITransactionProcessor transactionProcessor, BlockHeader blockHeader);
    UInt256 GetCandidateStake(BlockHeader blockHeader, Address candidate);
    UInt256 GetCandidateStake(ITransactionProcessor transactionProcessor, BlockHeader blockHeader, Address candidate);
    Address GetCandidateOwner(BlockHeader blockHeader, Address candidate);
    Address GetCandidateOwner(ITransactionProcessor transactionProcessor, BlockHeader blockHeader, Address candidate);
    Address GetCandidateOwner(IWorldState worldState, Address candidate);
}
