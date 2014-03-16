using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [DataContract(Name = "ValueSource", Namespace = "Evercoin.Storage.Model")]
    internal abstract class ValueSource : IValueSource
    {
        private const string SerializationName_AvailableValue = "AvailableValue";

        protected ValueSource()
        {
        }

        protected ValueSource(IValueSource copyFrom)
        {
            this.AvailableValue = copyFrom.AvailableValue;
        }

        /// <summary>
        /// Gets how much value can be spent by this source.
        /// </summary>
        [DataMember(Name = SerializationName_AvailableValue)]
        public decimal AvailableValue { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IValueSource other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.AvailableValue == other.AvailableValue;
        }
    }
}