using System.Threading;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Represents writable storage for a block / transaction chain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Notably absent from this API is the ability to fetch a block by its
    /// height, or the transactions associated with a given block.  Other
    /// types will have to be responsible for those concerns, since there are
    /// some nuances associated with those things.
    /// </para>
    /// <para>
    /// TODO: Also notably absent is the ability to "forget" about a block or
    /// transaction... no conscious reason, just something I haven't added yet.
    /// </para>
    /// <para>
    /// It is suggested that, instead of implementing this interface directly,
    /// you use <see cref="BaseImplementations.ReadWriteChainStoreBase"/> as a
    /// base class for implementations, since that provides error checking and
    /// boilerplate stuff like the pass-through overloads that don't require
    /// <see cref="CancellationToken"/> values to be passed in.
    /// </para>
    /// </remarks>
    public interface IChainStore : IReadableChainStore
    {
        /// <summary>
        /// Stores a block with the given identifier in this chain store.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to store.
        /// </param>
        /// <param name="block">
        /// The block to store.
        /// </param>
        void PutBlock(FancyByteArray blockIdentifier, IBlock block);

        /// <summary>
        /// Stores a transaction with the given identifier in this chain store.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to store.
        /// </param>
        /// <param name="transaction">
        /// The transaction to store.
        /// </param>
        void PutTransaction(FancyByteArray transactionIdentifier, ITransaction transaction);

        /// <summary>
        /// Stores a block with the given identifier in this chain store,
        /// asynchronously.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to store.
        /// </param>
        /// <param name="block">
        /// The block to store.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task PutBlockAsync(FancyByteArray blockIdentifier, IBlock block);

        /// <summary>
        /// Stores a block with the given identifier in this chain store,
        /// asynchronously.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to store.
        /// </param>
        /// <param name="block">
        /// The block to store.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task PutBlockAsync(FancyByteArray blockIdentifier, IBlock block, CancellationToken token);

        /// <summary>
        /// Stores a transaction with the given identifier in this chain store,
        /// asynchronously.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to store.
        /// </param>
        /// <param name="transaction">
        /// The transaction to store.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task PutTransactionAsync(FancyByteArray transactionIdentifier, ITransaction transaction);

        /// <summary>
        /// Stores a transaction with the given identifier in this chain store,
        /// asynchronously.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to store.
        /// </param>
        /// <param name="transaction">
        /// The transaction to store.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task PutTransactionAsync(FancyByteArray transactionIdentifier, ITransaction transaction, CancellationToken token);
    }
}
