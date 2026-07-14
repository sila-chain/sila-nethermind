using Nethermind.Core.Specs;
using Nethermind.Specs;

namespace Sila.Test.Base
{
    public abstract class SilaTest
    {
        public string? Category { get; set; }
        public string? Name { get; set; }
        public string? LoadFailure { get; set; }
        public ulong ChainId { get; set; } = MainnetSpecProvider.Instance.ChainId;
        public IReleaseSpec GenesisSpec => ChainId == MainnetSpecProvider.Instance.ChainId
            ? MainnetSpecProvider.Instance.GenesisSpec
            : GnosisSpecProvider.Instance.GenesisSpec;
    }
}
