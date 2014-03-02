using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace Evercoin.Network
{
    internal sealed class NetworkTransactionValueSource : NetworkValueSource, ITransactionValueSource
    {
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(ITransactionValueSource other)
        {
            return base.Equals(other) &&
                   this.OriginatingTransactionIdentifier == other.OriginatingTransactionIdentifier &&
                   this.OriginatingTransactionOutputIndex == other.OriginatingTransactionOutputIndex &&
                   this.ScriptPublicKey.SequenceEqual(other.ScriptPublicKey);
        }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        public BigInteger OriginatingTransactionIdentifier { get; set; }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        public uint OriginatingTransactionOutputIndex { get; set; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        public ImmutableList<byte> ScriptPublicKey { get; set; }
    }
}
