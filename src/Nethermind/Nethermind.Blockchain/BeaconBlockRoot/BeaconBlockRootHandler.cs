// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Sip2930;
using Nethermind.Core.Specs;
using Nethermind.Crypto;
using Nethermind.Savm.State;
using Nethermind.Savm.Tracing;
using Nethermind.Savm.TransactionProcessing;
using Nethermind.Int256;

namespace Nethermind.Blockchain.BeaconBlockRoot;

public class BeaconBlockRootHandler(ITransactionProcessor processor, IWorldState stateProvider) : IBeaconBlockRootHandler
{
    private const ulong GasLimit = 30_000_000UL;

    AccessList? IHasAccessList.GetAccessList(Block block, IReleaseSpec spec)
        => BeaconRootsAccessList(block, spec, includeStorageCells: true).accessList;

    public (Address? toAddress, AccessList? accessList) BeaconRootsAccessList(Block block, IReleaseSpec spec, bool includeStorageCells = false)
    {
        const int HistoryBufferLength = 8191;

        BlockHeader? header = block.Header;
        bool canInsertBeaconRoot = spec.IsBeaconBlockRootAvailable
                                  && !header.IsGenesis
                                  && header.ParentBeaconBlockRoot is not null;

        Address? sip4788ContractAddress = canInsertBeaconRoot ?
            spec.Sip4788ContractAddress ?? Sip4788Constants.BeaconRootsAddress :
            null;

        if (sip4788ContractAddress is null || !stateProvider.AccountExists(sip4788ContractAddress))
        {
            return (null, null);
        }

        AccessList.Builder builder = new AccessList.Builder()
            .AddAddress(sip4788ContractAddress);

        if (includeStorageCells)
        {
            // https://sips.sila.org/SIPS/sip-4788
            // Set the storage value at header.timestamp % HISTORY_BUFFER_LENGTH to be header.timestamp
            ulong slotIndex = header.Timestamp % HistoryBufferLength;
            UInt256 slot256 = slotIndex;
            builder.AddStorage(in slot256);
            // Set the storage value at header.timestamp % HISTORY_BUFFER_LENGTH + HISTORY_BUFFER_LENGTH to be calldata[0:32]
            slot256 = slotIndex + HistoryBufferLength;
            builder.AddStorage(in slot256);
        }

        return (sip4788ContractAddress, builder.Build());
    }

    public void StoreBeaconRoot(Block block, IReleaseSpec spec, ITxTracer tracer)
    {
        (Address? toAddress, AccessList? accessList) = BeaconRootsAccessList(block, spec, includeStorageCells: false);

        if (toAddress is not null)
        {
            BlockHeader? header = block.Header;
            Transaction transaction = new()
            {
                Value = 0,
                Data = header.ParentBeaconBlockRoot.Bytes.ToArray(),
                To = toAddress,
                SenderAddress = Address.SystemUser,
                GasLimit = GasLimit,
                GasPrice = 0,
                AccessList = accessList
            };

            transaction.Hash = transaction.CalculateHash();

            processor.Execute(transaction, tracer);
        }
    }
}
