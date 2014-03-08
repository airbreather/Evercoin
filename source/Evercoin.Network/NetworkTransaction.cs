using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace Evercoin.Network
{
    internal sealed class NetworkTransaction : ITransaction
    {
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(ITransaction other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            NetworkTransaction otherNetworkTransaction = other as NetworkTransaction;
            if (otherNetworkTransaction != null)
            {
                return this.Identifier == other.Identifier;
            }

            return other != null &&
                   this.Identifier == other.Identifier &&
                   this.Inputs.SequenceEqual(other.Inputs) &&
                   this.Outputs.SequenceEqual(other.Outputs) &&
                   this.Version == other.Version &&
                   this.LockTime == other.LockTime;
        }

        /// <summary>
        /// Gets a string that identifies this transaction.
        /// </summary>
        public BigInteger Identifier { get; set; }

        /// <summary>
        /// Gets the identifier of the <see cref="IBlock"/> that contains
        /// this transaction, if any.
        /// <see cref="string.Empty"/> if it is not yet included in a block.
        /// </summary>
        public BigInteger ContainingBlockIdentifier { get; set; }

        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        public ImmutableList<IValueSpender> Inputs { get; set; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        public ImmutableList<ITransactionValueSource> Outputs { get; set; }

        public uint LockTime { get; set; }
    }
}
