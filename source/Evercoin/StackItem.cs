using System;
using System.Collections.Generic;
using System.Numerics;

namespace Evercoin
{
    /// <summary>
    /// An item that can be put on the stack.
    /// </summary>
    public struct StackItem : IEquatable<StackItem>, IComparable<StackItem>, IComparable
    {
        private byte[] data;

        private BigInteger? value;

        public StackItem(BigInteger value)
            : this()
        {
            this.value = value;
        }

        public StackItem(IEnumerable<byte> data)
            : this()
        {
            this.data = data.GetArray();
        }

        public static implicit operator BigInteger(StackItem item)
        {
            return item.value ?? (item.value = new BigInteger(item.data)).Value;
        }

        public static implicit operator bool(StackItem item)
        {
            return !((BigInteger)item).IsZero;
        }

        public static implicit operator byte[](StackItem item)
        {
            return item.data ?? (item.data = item.value.Value.ToLittleEndianUInt256Array());
        }

        public static implicit operator StackItem(BigInteger data)
        {
            return new StackItem(data);
        }

        public static implicit operator StackItem(byte[] data)
        {
            return new StackItem(data);
        }

        public static implicit operator StackItem(bool value)
        {
            return new StackItem(value ? 1 : 0);
        }

        public bool Equals(StackItem other)
        {
            return this.data.Equals(other.data);
        }

        public override int GetHashCode()
        {
            return ((BigInteger)this).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            StackItem? other = obj as StackItem?;
            return other.HasValue && this.Equals(other.Value);
        }

        public int CompareTo(StackItem other)
        {
            return ((BigInteger)this).CompareTo(other);
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
