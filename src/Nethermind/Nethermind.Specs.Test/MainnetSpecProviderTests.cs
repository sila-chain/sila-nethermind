// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Specs;
using Nethermind.Specs.Forks;
using NUnit.Framework;

namespace Nethermind.Specs.Test
{
    [TestFixture]
    public class MainnetSpecProviderTests
    {
        private readonly ISpecProvider _specProvider = MainnetSpecProvider.Instance;

        [TestCase(12_243_999ul, false)]
        [TestCase(12_244_000ul, true)]
        public void Berlin_sips(ulong blockNumber, bool isEnabled)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).IsSip2537Enabled, Is.EqualTo(false));
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).IsSip2565Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).IsSip2929Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).IsSip2930Enabled, Is.EqualTo(isEnabled));
            }
        }

        [TestCase(12_964_999ul, false)]
        [TestCase(12_965_000ul, true)]
        public void London_sips(ulong blockNumber, bool isEnabled)
        {
            if (isEnabled)
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).DifficultyBombDelay, Is.EqualTo(London.Instance.DifficultyBombDelay));
            else
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).DifficultyBombDelay, Is.EqualTo(Berlin.Instance.DifficultyBombDelay));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).IsSip1559Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).IsSip3198Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).IsSip3529Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec((ForkActivation)blockNumber).IsSip3541Enabled, Is.EqualTo(isEnabled));
            }
        }

        [TestCase(MainnetSpecProvider.ParisBlockNumber, MainnetSpecProvider.ShanghaiBlockTimestamp, false)]
        [TestCase(MainnetSpecProvider.ParisBlockNumber, MainnetSpecProvider.CancunBlockTimestamp, true)]
        public void Cancun_sips(ulong blockNumber, ulong timestamp, bool isEnabled)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip1153Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip4844Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip5656Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip4788Enabled, Is.EqualTo(isEnabled));
            }
            if (isEnabled)
            {
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).Sip4788ContractAddress, Is.Not.Null);
            }
            else
            {
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).Sip4788ContractAddress, Is.Null);
            }
        }

        [TestCase(MainnetSpecProvider.ParisBlockNumber, MainnetSpecProvider.CancunBlockTimestamp, false)]
        [TestCase(MainnetSpecProvider.ParisBlockNumber, MainnetSpecProvider.PragueBlockTimestamp, true)]
        public void Prague_sips(ulong blockNumber, ulong timestamp, bool isEnabled)
        {
            Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip2935Enabled, Is.EqualTo(isEnabled));
            if (isEnabled)
            {
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).Sip2935ContractAddress, Is.Not.Null);
            }
            else
            {
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).Sip2935ContractAddress, Is.Null);
            }
        }

        [TestCase(MainnetSpecProvider.ParisBlockNumber, MainnetSpecProvider.PragueBlockTimestamp, false)]
        [TestCase(MainnetSpecProvider.ParisBlockNumber, MainnetSpecProvider.OsakaBlockTimestamp, true)]
        public void Osaka_sips(ulong blockNumber, ulong timestamp, bool isEnabled)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip7594Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip7823Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip7825Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip7883Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip7918Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip7934Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip7939Enabled, Is.EqualTo(isEnabled));
                Assert.That(_specProvider.GetSpec(new ForkActivation(blockNumber, timestamp)).IsSip7951Enabled, Is.EqualTo(isEnabled));
            }
        }

        [Test]
        public void Dao_block_number_is_correct() => Assert.That(_specProvider.DaoBlockNumber, Is.EqualTo(1920000UL));
    }
}
