using System.Collections.ObjectModel;
using System.Linq;

using Evercoin.Util;

namespace Evercoin.BaseImplementations
{
    public sealed class TypedTransaction : ITransaction
    {
        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        public ReadOnlyCollection<IValueSpender> Inputs { get; set; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        public ReadOnlyCollection<IValueSource> Outputs { get; set; }

        public uint LockTime { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as ITransaction);
        }

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
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCodeBuilder.BeginHashCode()
                .MixHashCodeWith(this.Version)
                .MixHashCodeWith(this.LockTime)
                .MixHashCodeWithEnumerable(this.Inputs)
                .MixHashCodeWithEnumerable(this.Outputs);
        }
    }
}
