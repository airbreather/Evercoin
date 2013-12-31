using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.Network
{
    /// <summary>
    /// Represents an event-driven network, from this client's perspective.
    /// </summary>
    /// <remarks>
    /// TODO: Remove this remark.
    /// This is actually intended to work for both SPV and Full-Node operation,
    /// even in the filtering mode specified in BIP 37, via "dummy"
    /// placeholder transactions that only have just enough data to fit into
    /// the Merkle tree.
    /// </remarks>
    public interface INetwork
    {
        /// <summary>
        /// Occurs when <see cref="BestChainTail"/> is extended by one block.
        /// </summary>
        /// <remarks>
        /// At the instant in time when this event is raised,
        /// <see cref="BestChainTail"/> is guaranteed to be equal to the
        /// <see cref="BestChainExtendedEventArgs.NewBestChainTail"/>.
        /// </remarks>
        event EventHandler<BestChainExtendedEventArgs> BestChainExtended;

        /// <summary>
        /// Occurs when <see cref="BestChainTail"/> is replaced by a new
        /// <see cref="IBlockChainNode"/> that does not build off of the
        /// <see cref="IBlockChainNode"/> that was the previous best chain.
        /// </summary>
        event EventHandler<BestChainReplacedEventArgs> BestChainReplaced;

        /// <summary>
        /// Occurs when some <see cref="ITransaction"/> object is received
        /// on this network.
        /// </summary>
        event EventHandler<TransactionReceivedEventArgs> TransactionReceived;
        
        /// <summary>
        /// Gets the <see cref="INetworkParameters"/> object that defines the
        /// parameters that this network uses.
        /// </summary>
        INetworkParameters Parameters { get; }

        /// <summary>
        /// Gets the <see cref="IBlockChainNode"/> that is highest in the best
        /// chain on the network.
        /// </summary>
        IBlockChainNode BestChainTail { get; }

        /// <summary>
        /// Asynchronously broadcasts a block to the network.
        /// </summary>
        /// <param name="block">
        /// The <see cref="IBlockChainNode"/> to broadcast.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task BroadcastBlockAsync(IBlockChainNode block);

        /// <summary>
        /// Asynchronously broadcasts a transaction to the network.
        /// </summary>
        /// <param name="transaction">
        /// The <see cref="ITransaction"/> to broadcast.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task BroadcastTransactionAsync(ITransaction transaction);
    }
}
