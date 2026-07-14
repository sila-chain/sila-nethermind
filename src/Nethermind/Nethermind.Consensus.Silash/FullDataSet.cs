// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Consensus.Silash
{
    internal class FullDataSet : ISilashDataSet
    {
        private uint[][] Data { get; set; }

        public uint Size => (uint)(Data.Length * Silash.HashBytes);

        public FullDataSet(ulong setSize, ISilashDataSet cache)
        {
            Data = new uint[(uint)(setSize / Silash.HashBytes)][];
            for (uint i = 0; i < Data.Length; i++)
            {
                Data[i] = cache.CalcDataSetItem(i);
            }
        }

        public uint[] CalcDataSetItem(uint i) => Data[i];

        public void Dispose()
        {
        }
    }
}
