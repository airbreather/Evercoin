using System.Collections.Immutable;
using System.Linq;

namespace Evercoin.App
{
    internal sealed class SomeValueSourceClass : IValueSource
    {
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IValueSource other)
        {
            return other != null &&
                   this.AvailableValue == other.AvailableValue &&
                   this.ScriptPubKey.SequenceEqual(other.ScriptPubKey);
        }

        /// <summary>
        /// Gets how much value can be spent by this source.
        /// </summary>
        public decimal AvailableValue { get; set; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        public IImmutableList<byte> ScriptPubKey { get; set; }
    }
}
