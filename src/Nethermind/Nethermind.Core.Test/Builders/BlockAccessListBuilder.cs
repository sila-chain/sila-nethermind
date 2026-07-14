// SPDX-FileCopyrightText: 2026 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using Nethermind.Core.BlockAccessLists;
using Nethermind.Core.Crypto;
using Nethermind.Int256;

namespace Nethermind.Core.Test.Builders
{
    public class BlockAccessListBuilder : BuilderBase<ReadOnlyBlockAccessList>
    {
        private readonly SortedList<Address, ReadOnlyAccountChanges> _accounts = new(GenericComparer.GetOptimized<Address>());

        public BlockAccessListBuilder() => Rebuild();

        public BlockAccessListBuilder WithAccountChanges(params ReadOnlyAccountChanges[] accountChanges)
        {
            foreach (ReadOnlyAccountChanges a in accountChanges)
            {
                _accounts[a.Address] = a;
            }
            Rebuild();
            return this;
        }

        public BlockAccessListBuilder WithPrecompileChanges(Hash256 parentHash, ulong timestamp)
            => WithAccountChanges(
                Sip2935Changes(parentHash),
                Sip4788Changes(timestamp),
                Sip7002Changes,
                Sip7251Changes,
                Sip8282BuilderDepositChanges,
                Sip8282BuilderExitChanges);

        private static ReadOnlyAccountChanges Sip2935Changes(Hash256 parentHash)
        {
            StorageChange parentHashStorageChange = new(0, new UInt256(parentHash.BytesToArray(), isBigEndian: true));
            return Build.An.AccountChanges
                .WithAddress(Sip2935Constants.BlockHashHistoryAddress)
                .WithStorageChanges(0, parentHashStorageChange)
                .TestObject;
        }

        private static ReadOnlyAccountChanges Sip4788Changes(ulong timestamp)
        {
            UInt256 sip4788Slot1 = timestamp % Sip4788Constants.RingBufferSize;
            UInt256 sip4788Slot2 = (timestamp % Sip4788Constants.RingBufferSize) + Sip4788Constants.RingBufferSize;

            return Build.An.AccountChanges
                .WithAddress(Sip4788Constants.BeaconRootsAddress)
                .WithStorageChanges(sip4788Slot1, [new(0, timestamp)])
                .WithStorageReads(sip4788Slot2)
                .TestObject;
        }

        private static readonly ReadOnlyAccountChanges Sip7002Changes = Build.An.AccountChanges
            .WithAddress(Sip7002Constants.WithdrawalRequestPredeployAddress)
            .WithStorageReads(0, 1, 2, 3)
            .TestObject;

        private static readonly ReadOnlyAccountChanges Sip7251Changes = Build.An.AccountChanges
            .WithAddress(Sip7251Constants.ConsolidationRequestPredeployAddress)
            .WithStorageReads(0, 1, 2, 3)
            .TestObject;

        private static readonly ReadOnlyAccountChanges Sip8282BuilderDepositChanges = Build.An.AccountChanges
            .WithAddress(Sip8282Constants.BuilderDepositRequestPredeployAddress)
            .WithStorageReads(0, 1, 2, 3)
            .TestObject;

        private static readonly ReadOnlyAccountChanges Sip8282BuilderExitChanges = Build.An.AccountChanges
            .WithAddress(Sip8282Constants.BuilderExitRequestPredeployAddress)
            .WithStorageReads(0, 1, 2, 3)
            .TestObject;

        private void Rebuild()
        {
            ReadOnlyAccountChanges[] ordered = new ReadOnlyAccountChanges[_accounts.Count];
            int itemCount = 0;
            int i = 0;
            foreach (KeyValuePair<Address, ReadOnlyAccountChanges> kv in _accounts)
            {
                ordered[i++] = kv.Value;
                itemCount += 1 + kv.Value.StorageChanges.Length + kv.Value.StorageReads.Length;
            }
            TestObjectInternal = new ReadOnlyBlockAccessList(ordered, itemCount);
        }
    }
}
