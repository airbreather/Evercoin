using System;
using System.Numerics;

namespace Evercoin
{
    /// <summary>
    /// An item that can be put on the stack.
    /// </summary>
    public struct StackItem : IEquatable<StackItem>, IComparable<StackItem>, IComparable
    {
        private readonly FancyByteArray data;

        public StackItem(FancyByteArray data)
            : this()
        {
            this.data = data;
        }

        public static implicit operator BigInteger(StackItem item)
        {
            return item.data.NumericValue;
        }

        public static implicit operator bool(StackItem item)
        {
            return !item.data.NumericValue.IsZero;
        }

        public static implicit operator FancyByteArray(StackItem item)
        {
            return item.data;
        }

        public static implicit operator byte[](StackItem item)
        {
            return item.data.Value;
        }

        public static implicit operator StackItem(FancyByteArray data)
        {
            return new StackItem(data);
        }

        public static implicit operator StackItem(byte[] data)
        {
            return new StackItem(data);
        }

        public static implicit operator StackItem(bool value)
        {
            return new StackItem(FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(value ? 1 : 0, Endianness.LittleEndian));
        }

        public bool Equals(StackItem other)
        {
            return this.data.Equals(other.data);
        }

        public override int GetHashCode()
        {
            return this.data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            StackItem? other = obj as StackItem?;
            return other.HasValue && this.Equals(other.Value);
        }

        public int CompareTo(StackItem other)
        {
            return this.data.CompareTo(other.data);
        }

        public int CompareTo(object obj)
        {
            StackItem? other = obj as StackItem?;
            if (!other.HasValue)
            {
                throw new ArgumentException("Can only compare a StackItem with another StackItem.", "obj");
            }

            return this.CompareTo(other.Value);
        }

        public static bool operator <(StackItem first, StackItem second)
        {
            return first.CompareTo(second) < 0;
        }

        public static bool operator >(StackItem first, StackItem second)
        {
            return first.CompareTo(second) > 0;
        }

        public static bool operator <=(StackItem first, StackItem second)
        {
            return first.CompareTo(second) <= 0;
        }

        public static bool operator >=(StackItem first, StackItem second)
        {
            return first.CompareTo(second) >= 0;
        }

        public static bool operator ==(StackItem first, StackItem second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(StackItem first, StackItem second)
        {
            return !first.Equals(second);
        }
    }
}
