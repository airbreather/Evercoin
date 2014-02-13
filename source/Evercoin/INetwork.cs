using System;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Represents the observable cryptocurrency network.
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
        /// Gets an observable sequence of <see cref="IBlock"/> objects
        /// received on this network.
        /// </summary>
        /// <remarks>
        /// The <see cref="IBlock"/> objects are guaranteed to be
        /// populated, but may not actually be valid according to the best
        /// blockchain.
        /// </remarks>
        IObservable<IBlock> ReceivedBlocks { get; }

        /// <summary>
        /// Gets an observable sequence of <see cref="ITransaction"/> objects
        /// received on this network.
        /// </summary>
        /// <remarks>
        /// The <see cref="ITransaction"/> objects are guaranteed to be
        /// populated, but may not actually be valid according to the best
        /// blockchain.
        /// </remarks>
        IObservable<ITransaction> ReceivedTransactions { get; }

        /// <summary>
        /// Gets the <see cref="INetworkParameters"/> object that defines the
        /// parameters that this network uses.
        /// </summary>
        INetworkParameters Parameters { get; }

        /// <summary>
        /// Asynchronously broadcasts a blockchain node to the network.
        /// </summary>
        /// <param name="block">
        /// The <see cref="IBlock"/> to broadcast.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task BroadcastBlockAsync(IBlock block);

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
