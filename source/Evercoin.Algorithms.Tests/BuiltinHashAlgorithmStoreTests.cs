using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Moq;

using Xunit;
using Xunit.Extensions;

namespace Evercoin.Algorithms
{
    public sealed class BuiltinHashAlgorithmStoreTests
    {
        public static IEnumerable<object[]> KnownHashAlgorithms
        {
            get
            {
                return new[]
                {
                    new object[] { HashAlgorithmIdentifiers.DoubleSHA256 },
                    new object[] { HashAlgorithmIdentifiers.LitecoinSCrypt },
                    new object[] { HashAlgorithmIdentifiers.RipeMd160 },
                    new object[] { HashAlgorithmIdentifiers.SHA1 },
                    new object[] { HashAlgorithmIdentifiers.SHA256 },
                    new object[] { HashAlgorithmIdentifiers.SHA256ThenRipeMd160 }
                };
            }
        }

        [Theory]
        [PropertyData("KnownHashAlgorithms")]
        public void GetHashAlgorithmShouldReturnKnownAlgorithm(Guid knownAlgorithmIdentifier)
        {
            BuiltinHashAlgorithmStore sut = new BuiltinHashAlgorithmStore();

            IHashAlgorithm algorithm = sut.GetHashAlgorithm(knownAlgorithmIdentifier);

            Assert.NotNull(algorithm);
        }

        [Fact]
        public void GetHashAlgorithmShouldFailOnUnknownAlgorithm()
        {
            BuiltinHashAlgorithmStore sut = new BuiltinHashAlgorithmStore();
            Guid unknownIdentifier = Guid.NewGuid();

            Assert.Throws<KeyNotFoundException>(() => sut.GetHashAlgorithm(unknownIdentifier));
        }

        [Fact]
        public void RegisterHashAlgorithmShouldFail()
        {
            IHashAlgorithmStore sut = new BuiltinHashAlgorithmStore();
            Guid someGuid = Guid.NewGuid();
            IHashAlgorithm someAlgorithm = Mock.Of<IHashAlgorithm>();

            Assert.Throws<NotSupportedException>(() => sut.RegisterHashAlgorithm(someGuid, someAlgorithm));
        }
    }
}
