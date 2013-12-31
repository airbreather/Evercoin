using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.Network
{
    /// <summary>
    /// Represents the arguments for an event raised when the best chain is
    /// extended by one <see cref="IBlockChainNode"/>.
    /// </summary>
    public sealed class BestChainExtendedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BestChainExtendedEventArgs"/> class.
        /// </summary>
        /// <param name="newBestChainTail">
        /// The <see cref="IBlockChainNode"/> that is the new tail of the best chain.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="newBestChainTail"/> is <c>null</c>.
        /// </exception>
        public BestChainExtendedEventArgs(IBlockChainNode newBestChainTail)
        {
            if (newBestChainTail == null)
            {
                throw new ArgumentNullException("newBestChainTail");
            }

            this.NewBestChainTail = newBestChainTail;
        }

        /// <summary>
        /// Gets the <see cref="IBlockChainNode"/> that is the new tail of the
        /// best chain.
        /// </summary>
        public IBlockChainNode NewBestChainTail { get; private set; }
    }
}
