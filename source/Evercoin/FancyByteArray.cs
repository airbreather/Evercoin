using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Evercoin.Util;

namespace Evercoin
{
    public struct FancyByteArray : IEquatable<FancyByteArray>, IComparable<FancyByteArray>, IComparable
    {
        private static readonly byte[] EmptyByteArray = new byte[0];

        private readonly byte[] underlyingArray;

        private FancyByteArray(byte[] underlyingArray)
            : this()
        {
            this.underlyingArray = underlyingArray;
        }

        public byte[] Value { get { return this.underlyingArray ?? EmptyByteArray; } }

        public BigInteger NumericValue { get { return new BigInteger(this.Value); } }

        public static implicit operator byte[](FancyByteArray fancyByteArray)
        {
            return fancyByteArray.Value;
        }

        public static implicit operator BigInteger(FancyByteArray fancyByteArray)
        {
            return fancyByteArray.NumericValue;
        }

        public static implicit operator FancyByteArray(byte[] data)
        {
            return CreateFromBytes(data);
        }

        public static FancyByteArray CreateFromBytes(IEnumerable<byte> bytes)
        {
            bytes = bytes ?? Enumerable.Empty<byte>();
            return new FancyByteArray(bytes.GetArray());
        }

        public static FancyByteArray CreateFromBigIntegerWithDesiredEndianness(BigInteger bigInteger, Endianness desiredEndianness)
        {
            return CreateFromBigIntegerWithDesiredLengthAndEndianness(bigInteger, null, desiredEndianness);
        }

        public static FancyByteArray CreateFromBigIntegerWithDesiredLengthAndEndianness(BigInteger bigInteger, int desiredLengthInBytes, Endianness desiredEndianness)
        {
            return CreateFromBigIntegerWithDesiredLengthAndEndianness(bigInteger, desiredLengthInBytes, desiredEndianness);
        }

        private static FancyByteArray CreateFromBigIntegerWithDesiredLengthAndEndianness(BigInteger bigInteger, int? desiredLengthInBytes, Endianness desiredEndianness)
        {
            if (desiredLengthInBytes <= 0)
            {
                throw new ArgumentOutOfRangeException("desiredLengthInBytes", desiredLengthInBytes, "Must be at least 1.");
            }

            byte[] unpaddedLittleEndianResult = bigInteger.ToByteArray();

            if (unpaddedLittleEndianResult.Length > desiredLengthInBytes)
            {
                throw new ArgumentOutOfRangeException("desiredLengthInBytes",
                                                      desiredLengthInBytes,
                                                      "The most compact representation of " + bigInteger.ToString(CultureInfo.InvariantCulture) +
                                                      " requires at least " + unpaddedLittleEndianResult.Length.ToString(CultureInfo.InvariantCulture) + " bytes.");
            }

            byte[] littleEndianResultWithDesiredLength;
            if (!desiredLengthInBytes.HasValue ||
                unpaddedLittleEndianResult.Length == desiredLengthInBytes)
            {
                littleEndianResultWithDesiredLength = unpaddedLittleEndianResult;
            }
            else
            {
                littleEndianResultWithDesiredLength = new byte[desiredLengthInBytes.Value];
                Buffer.BlockCopy(unpaddedLittleEndianResult, 0, littleEndianResultWithDesiredLength, 0, desiredLengthInBytes.Value);

                if (bigInteger.Sign < 0)
                {
                    // We effectively appended 0-valued bits up to the length.
                    // When the value is negative, it needs to be 1-valued bits,
                    // otherwise we've changed the two's-complement value.
                    for (int i = unpaddedLittleEndianResult.Length; i < desiredLengthInBytes; i++)
                    {
                        littleEndianResultWithDesiredLength[i] = 0xFF;
                    }
                }
            }

            byte[] result;
            switch (desiredEndianness)
            {
                case Endianness.BigEndian:
                    result = littleEndianResultWithDesiredLength;
                    Array.Reverse(result);

                    // littleEndianResultWithDesiredLength also technically got
                    // reversed by doing this, but we're done with it anyway.
                    break;

                case Endianness.LittleEndian:
                    result = littleEndianResultWithDesiredLength;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("desiredEndianness", desiredEndianness, "Unrecognized value.");
            }

            return new FancyByteArray(result);
        }

        public static bool operator ==(FancyByteArray first, FancyByteArray second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(FancyByteArray first, FancyByteArray second)
        {
            return !first.Equals(second);
        }

        public static bool operator <(FancyByteArray first, FancyByteArray second)
        {
            return first.CompareTo(second) < 0;
        }

        public static bool operator >(FancyByteArray first, FancyByteArray second)
        {
            return first.CompareTo(second) > 0;
        }

        public static bool operator <=(FancyByteArray first, FancyByteArray second)
        {
            return first.CompareTo(second) <= 0;
        }

        public static bool operator >=(FancyByteArray first, FancyByteArray second)
        {
            return first.CompareTo(second) >= 0;
        }

        public override string ToString()
        {
            return ByteTwiddling.ByteArrayToHexString(this.Value);
        }

        public override bool Equals(object obj)
        {
            FancyByteArray? other = obj as FancyByteArray?;
            return other.HasValue &&
                   this.Equals(other.Value);
        }

        public bool Equals(FancyByteArray other)
        {
            BigInteger thisValue = this.NumericValue;
            BigInteger otherValue = other.NumericValue;

            return thisValue == otherValue;
        }

        public override int GetHashCode()
        {
            HashCodeBuilder builder = new HashCodeBuilder()
                .HashWith(this.NumericValue);

            return builder;
        }

        public int CompareTo(FancyByteArray other)
        {
            BigInteger thisValue = this.NumericValue;
            BigInteger otherValue = other.NumericValue;

            return thisValue.CompareTo(otherValue);
        }

        int IComparable.CompareTo(object obj)
        {
            FancyByteArray? other = obj as FancyByteArray?;
            if (!other.HasValue)
            {
                throw new ArgumentException("Can only compare with instances of " + this.GetType().Name, "obj");
            }

            return this.CompareTo(other.Value);
        }
    }
}
