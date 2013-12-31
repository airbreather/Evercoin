﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.TransactionScript
{
    /// <summary>
    /// An item that can be put on the stack.
    /// </summary>
    internal struct StackItem : IEquatable<StackItem>
    {
        /// <summary>
        /// The underlying data item being stored on the stack.
        /// </summary>
        private readonly BigInteger data;

        public StackItem(BigInteger value)
            : this()
        {
            this.data = value;
        }

        public StackItem(byte[] value)
            : this(new BigInteger(value))
        {
        }

        public static implicit operator ulong(StackItem item)
        {
            return (ulong)item.data;
        }

        public static implicit operator bool(StackItem item)
        {
            return !item.data.IsZero;
        }

        public static implicit operator byte[](StackItem item)
        {
            return item.data.ToByteArray();
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
    }
}
