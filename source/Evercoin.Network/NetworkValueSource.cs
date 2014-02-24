using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Evercoin.Util;

namespace Evercoin.Network
{
    internal class NetworkValueSource : IValueSource
    {
        private readonly List<byte> scriptPubKey = new List<byte>();

        public NetworkValueSource()
        {
        }

        public NetworkValueSource(IValueSource valueSource)
        {
            this.InitFrom(valueSource);
        }

            /// <summary>
        /// Gets or sets how much value can be spent by this source.
        /// </summary>
        public decimal AvailableValue { get; set; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        public IList<byte> ScriptPubKey { get { return this.scriptPubKey; } }

        IImmutableList<byte> IValueSource.ScriptPubKey { get { return ImmutableList.CreateRange(this.scriptPubKey); } } 

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public virtual bool Equals(IValueSource other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.AvailableValue == other.AvailableValue &&
                   this.ScriptPubKey.SequenceEqual(other.ScriptPubKey);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as IValueSource);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return new HashCodeBuilder()
                .HashWith(this.AvailableValue)
                .HashWith(this.ScriptPubKey.Count);
        }

        private void InitFrom(IValueSource valueSource)
        {
            this.AvailableValue = valueSource.AvailableValue;
            this.scriptPubKey.AddRange(valueSource.ScriptPubKey);
        }
    }
}