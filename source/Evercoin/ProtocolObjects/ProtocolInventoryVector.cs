using System;
using System.Numerics;

using Evercoin.Util;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolInventoryVector
    {
        public ProtocolInventoryVector(InventoryType type, FancyByteArray hash)
        {
            this.Type = type;
            this.Hash = hash;
        }

        public InventoryType Type { get; private set; }

        public FancyByteArray Hash { get; private set; }

        public byte[] Data
        {
            get
            {
                byte[] typeBytes = BitConverter.GetBytes((uint)this.Type)
                                               .LittleEndianToOrFromBitConverterEndianness();
                byte[] hashBytes = this.Hash;

                return ByteTwiddling.ConcatenateData(typeBytes, hashBytes);
            }
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
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            ProtocolInventoryVector other = obj as ProtocolInventoryVector;
            return other != null &&
                   this.Type == other.Type &&
                   this.Hash == other.Hash;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            HashCodeBuilder builder = new HashCodeBuilder()
                .HashWith(this.Type)
                .HashWith(this.Hash);
            return builder;
        }

        public enum InventoryType : uint
        {
            Transaction = 1,
            Block = 2
        }
    }
}
