using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network
{
    /// <summary>
    /// Represents the arguments for an event raised when the best chain is
    /// replaced by a new <see cref="IBlockChainNode"/>.
    /// </summary>
    public sealed class BestChainReplacedEventArgs : EventArgs
    {
        /// <summary>
        /// An object to synchronize calls to <see cref="FindSharedParent"/>.
        /// </summary>
        private readonly object findSharedParentLock = new object();

        /// <summary>
        /// A value indicating whether <see cref="sharedParent"/> has been
        /// determined.
        /// </summary>
        /// <remarks>
        /// Can't check for <c>null</c>, because that means something special.
        /// </remarks>
        private bool sharedParentIsAvailable;

        /// <summary>
        /// Backing store for <see cref="SharedParent"/>.
        /// </summary>
        private IBlockChainNode sharedParent;

        /// <summary>
        /// Initializes a new instance of the <see cref="BestChainReplacedEventArgs"/> class.
        /// </summary>
        /// <param name="oldBestChainTail">
        /// The <see cref="IBlockChainNode"/> that was the old tail of the best chain.
        /// </param>
        /// <param name="newBestChainTail">
        /// The <see cref="IBlockChainNode"/> that is the new tail of the best chain.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldBestChainTail"/> is <c>null</c>, or
        /// <paramref name="newBestChainTail"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="oldBestChainTail"/> equals <see cref="newBestChainTail"/>.
        /// </exception>
        public BestChainReplacedEventArgs(IBlockChainNode oldBestChainTail, IBlockChainNode newBestChainTail)
        {
            if (oldBestChainTail == null)
            {
                throw new ArgumentNullException("oldBestChainTail");
            }

            if (newBestChainTail == null)
            {
                throw new ArgumentNullException("newBestChainTail");
            }

            if (oldBestChainTail.Equals(newBestChainTail))
            {
                throw new ArgumentException("cannot replace the best chain");
            }

            this.OldBestChainTail = oldBestChainTail;
            this.NewBestChainTail = newBestChainTail;
        }

        /// <summary>
        /// Gets the highest <see cref="IBlockChainNode"/> that both
        /// <see cref="OldBestChainTail"/> and <see cref="NewBestChainTail"/>
        /// have as a common ancestor.
        /// </summary>
        /// <remarks>
        /// If the chains disagree at the head (genesis block), then this will
        /// be <c>null</c>.
        /// </remarks>
        public IBlockChainNode SharedParent
        {
            get
            {
                if (!this.sharedParentIsAvailable)
                {
                    lock (this.findSharedParentLock)
                    {
                        if (!this.sharedParentIsAvailable)
                        {
                            this.sharedParent = this.FindSharedParent();
                            this.sharedParentIsAvailable = true;
                        }
                    }
                }

                return this.sharedParent;
            }
        }

        /// <summary>
        /// Gets the <see cref="IBlockChainNode"/> that was the tail of the old
        /// best chain.
        /// </summary>
        public IBlockChainNode OldBestChainTail { get; private set; }

        /// <summary>
        /// Gets the <see cref="IBlockChainNode"/> that is the tail of the new
        /// best chain.
        /// </summary>
        public IBlockChainNode NewBestChainTail { get; private set; }

        /// <summary>
        /// Finds the highest <see cref="IBlockChainNode"/> that both
        /// <see cref="OldBestChainTail"/> and <see cref="NewBestChainTail"/>
        /// have as a common ancestor.
        /// </summary>
        /// <returns>
        /// The highest <see cref="IBlockChainNode"/> that both
        /// <see cref="OldBestChainTail"/> and <see cref="NewBestChainTail"/>
        /// have as a common ancestor.
        /// </returns>
        private IBlockChainNode FindSharedParent()
        {
            IBlockChainNode oldChainNode = this.OldBestChainTail;
            IBlockChainNode newChainNode = this.NewBestChainTail;

            // The common ancestor won't be any higher
            // than the height of the lowest tail.
            ulong currentHeight = Math.Min(oldChainNode.Height, newChainNode.Height);

            const string BadImplementationMessage = "Implement IBlockChainNode properly.  " +
                "Height must indicate how many non-null PreviousNodes there are before this node.";

            while (newChainNode.Height > currentHeight)
            {
                // The new chain is higher than the old chain.
                newChainNode = newChainNode.PreviousNode;
                Debug.Assert(newChainNode != null, BadImplementationMessage);
            }

            while (oldChainNode.Height > currentHeight)
            {
                // The old chain is higher than the new chain.
                // This could happen if the old chain contains more blocks,
                // but at a lower aggregate difficulty than the new chain.
                oldChainNode = oldChainNode.PreviousNode;
                Debug.Assert(oldChainNode != null, BadImplementationMessage);
            }

            do
            {
                Debug.Assert(oldChainNode != null, BadImplementationMessage);
                Debug.Assert(newChainNode != null, BadImplementationMessage);

                if (newChainNode.Equals(oldChainNode))
                {
                    return newChainNode;
                }

                oldChainNode = oldChainNode.PreviousNode;
                newChainNode = newChainNode.PreviousNode;
            }
            while (currentHeight-- > 0);

            // If we've gotten here, then that means that the chains share no
            // common ancestors, including the genesis block.
            return null;
        }
    }
}
