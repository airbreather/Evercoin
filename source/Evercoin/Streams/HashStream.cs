using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.Streams
{
    /// <summary>
    /// A <see cref="Stream"/> that lazily calculates the hash of an
    /// <see cref="IHashTreeNode"/> on read.
    /// </summary>
    internal sealed class HashStream : BaseStream
    {
        private readonly IHashTreeNode treeNode;

        private readonly IHashAlgorithm algorithm;

        private Stream resultStream;

        public HashStream(IHashTreeNode treeNode, IHashAlgorithm algorithm)
        {
            if (treeNode == null)
            {
                throw new ArgumentNullException("treeNode");
            }

            if (algorithm == null)
            {
                throw new ArgumentNullException("algorithm");
            }

            this.treeNode = treeNode;
            this.algorithm = algorithm;
        }

        public override bool CanRead
        {
            get { return !this.IsDisposed; }
        }

        protected override int ReadInternal(byte[] buffer, int offset, int count)
        {
            if (this.resultStream == null)
            {
                this.resultStream = this.treeNode.CalculateHash(this.algorithm);
            }

            return this.resultStream.Read(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.resultStream != null)
                {
                    this.resultStream.Dispose();
                    this.resultStream = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
