using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using Evercoin.Util;

namespace Evercoin
{
    public struct TransactionScriptOperation : IEquatable<TransactionScriptOperation>
    {
        public static readonly TransactionScriptOperation Invalid = new TransactionScriptOperation();

        private readonly bool isValid;

        private readonly byte opcode;

        private readonly ImmutableList<byte> data;

        public TransactionScriptOperation(byte opcode)
            : this(opcode, Enumerable.Empty<byte>())
        {
        }

        public TransactionScriptOperation(byte opcode, IEnumerable<byte> data)
            : this()
        {
            this.isValid = true;
            this.opcode = opcode;
            this.data = data.ToImmutableList();
        }

        public bool IsValid { get { return this.isValid; } }

        public byte Opcode
        {
            get
            {
                Debug.Assert(this.IsValid, "Script operation is not valid.");
                return this.opcode;
            }
        }

        public ImmutableList<byte> Data
        {
            get
            {
                Debug.Assert(this.IsValid, "Script operation is not valid.");
                return this.data;
            }
        }

        public static implicit operator TransactionScriptOperation(byte opcode)
        {
            return new TransactionScriptOperation(opcode);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(TransactionScriptOperation other)
        {
            return this.isValid == other.isValid &&
                   this.opcode == other.opcode &&
                   (this.data == null) == (other.data == null) &&
                   (this.data != null && other.data != null &&
                    this.data.SequenceEqual(other.data));
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>
        /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name="obj">Another object to compare to. </param>
        public override bool Equals(object obj)
        {
            TransactionScriptOperation? other = obj as TransactionScriptOperation?;
            return other.HasValue &&
                   this.Equals(other.Value);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            HashCodeBuilder builder = new HashCodeBuilder()
                .HashWith(this.isValid)
                .HashWith(this.opcode);

            return this.data == null ?
                   builder.HashWith(this.data) :
                   this.data.Aggregate(builder, (prevBuilder, nextByte) => prevBuilder.HashWith(nextByte));
        }
    }
}
