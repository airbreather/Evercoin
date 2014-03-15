using System.Linq;

namespace Evercoin.BaseImplementations
{
    internal sealed class TypedTransaction : ITransaction
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

            return other != null &&
                   this.Inputs.SequenceEqual(other.Inputs) &&
                   this.Outputs.SequenceEqual(other.Outputs) &&
                   this.Version == other.Version &&
                   this.LockTime == other.LockTime;
        }

        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        public IValueSpender[] Inputs { get; set; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        public ITransactionValueSource[] Outputs { get; set; }

        public uint LockTime { get; set; }
    }
}
