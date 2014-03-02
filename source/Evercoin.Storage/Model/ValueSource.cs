using System;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [Serializable]
    internal abstract class ValueSource : IValueSource, ISerializable
    {
        private const string SerializationName_AvailableValue = "AvailableValue";

        protected ValueSource()
        {
        }

        protected ValueSource(IValueSource copyFrom)
        {
            this.AvailableValue = copyFrom.AvailableValue;
        }

        protected ValueSource(SerializationInfo info, StreamingContext context)
        {
            this.AvailableValue = info.GetDecimal(SerializationName_AvailableValue);
        }

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

        /// <summary>
        /// Gets how much value can be spent by this source.
        /// </summary>
        public decimal AvailableValue { get; set; }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(SerializationName_AvailableValue, this.AvailableValue);
        }
    }
}