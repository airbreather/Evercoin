using System;
using System.Collections.Generic;
using System.Linq;

using Evercoin.Util;

namespace Evercoin
{
    public struct TransactionScriptOperation : IEquatable<TransactionScriptOperation>
    {
        public static readonly TransactionScriptOperation Invalid = new TransactionScriptOperation();

        private readonly bool isValid;

        private readonly byte opcode;

        private readonly FancyByteArray data;

        public TransactionScriptOperation(byte opcode)
            : this(opcode, Enumerable.Empty<byte>())
        {
        }

        public TransactionScriptOperation(byte opcode, IEnumerable<byte> data)
            : this()
        {
            this.isValid = true;
            this.opcode = opcode;
            this.data = FancyByteArray.CreateFromBytes(data);
        }

        public bool IsValid { get { return this.isValid; } }

        public byte Opcode { get { return this.opcode; } }

        public FancyByteArray Data { get { return this.data; } }

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
            return this.opcode == other.opcode &&
                   this.data == other.data;
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
                .HashWith(this.opcode)
                .HashWith(this.data);

            return builder;
        }
    }
}
