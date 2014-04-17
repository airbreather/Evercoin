using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Evercoin.Util
{
    internal struct ReadOnlySubList<T> : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> source;

        private readonly int offset;

        private readonly int count;

        public ReadOnlySubList(IReadOnlyList<T> source, int offset, int count)
            : this()
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", count, "count can't be negative");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", offset, "offset can't be negative");
            }

            if (offset + count > source.Count)
            {
                throw new ArgumentException("the subset of <source> beginning at <offset> contains fewer than <count> elements.", "count");
            }

            this.source = source;
            this.offset = offset;
            this.count = count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public int Count { get { return this.count; } }

        public T this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index", index, "must be non-negative");
                }

                if (index >= this.count)
                {
                    throw new ArgumentOutOfRangeException("index", index, "must be less than " + this.count.ToString(CultureInfo.InvariantCulture));
                }

                return this.source[index + offset];
            }
        }

        private sealed class Enumerator : IEnumerator<T>
        {
            private readonly ReadOnlySubList<T> parent;

            private int currentIndex;

            public Enumerator(ReadOnlySubList<T> parent)
            {
                this.parent = parent;
                this.currentIndex = -1;
            }

            public T Current { get { return this.parent[this.currentIndex]; } }

            object IEnumerator.Current { get { return this.Current; } }

            public bool MoveNext() { return ++this.currentIndex < this.parent.Count; }

            public void Reset() { this.currentIndex = -1; }

            void IDisposable.Dispose() { }
        }
    }
}
