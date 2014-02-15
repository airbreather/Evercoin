using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Evercoin.TransactionScript
{
    internal sealed class ScriptEnumerator : IEnumerator<byte>
    {
        private readonly List<byte> dataSinceLastSep = new List<byte>(); 
        private readonly IEnumerator<byte> underlyingEnumerator;

        public ScriptEnumerator(IEnumerator<byte> underlyingEnumerator)
        {
            if (underlyingEnumerator == null)
            {
                throw new ArgumentNullException("underlyingEnumerator");
            }

            this.underlyingEnumerator = underlyingEnumerator;
        }

        public byte Current { get { return this.underlyingEnumerator.Current; } }

        public ImmutableList<byte> DataSinceLastSep { get { return ImmutableList.CreateRange(this.dataSinceLastSep); } }

        public void Dispose()
        {
            this.underlyingEnumerator.Dispose();
        }

        object IEnumerator.Current { get { return this.Current; } }

        public bool MoveNext()
        {
            bool ret = this.underlyingEnumerator.MoveNext();
            if (ret)
            {
                this.dataSinceLastSep.Add(this.underlyingEnumerator.Current);
            }

            return ret;
        }
        
        public void Reset()
        {
            this.underlyingEnumerator.Reset();
        }

        public void Sep()
        {
            this.dataSinceLastSep.Clear();
        }
    }
}
