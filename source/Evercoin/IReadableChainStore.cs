using System;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Represents readable storage for a block / transaction chain.
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
    public interface IReadableChainStore : IDisposable
    {
        /// <summary>
        /// Gets a block with the given identifier from this chain store.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to get.
        /// </param>
        /// <returns>
        /// The block with the given identifier, or <c>null</c> if the
        /// block could not be found.
        /// </returns>
        /// <remarks>
        /// It is permitted for a call to this method to block indefinitely
        /// while waiting for a block with the given identifier to exist,
        /// rather than return <c>null</c> immediately.
        /// </remarks>
        IBlock GetBlock(FancyByteArray blockIdentifier);

        /// <summary>
        /// Gets a transaction with the given identifier from this chain store.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to get.
        /// </param>
        /// <returns>
        /// The transaction with the given identifier, or <c>null</c> if the
        /// transaction could not be found.
        /// </returns>
        /// <remarks>
        /// It is permitted for a call to this method to block indefinitely
        /// while waiting for a transaction with the given identifier to exist,
        /// rather than return <c>null</c> immediately.
        /// </remarks>
        ITransaction GetTransaction(FancyByteArray transactionIdentifier);

        /// <summary>
        /// Indicates whether this chain store currently contains
        /// a block with the given identifier.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to search for.
        /// </param>
        /// <returns>
        /// A value indicating whether this chain store currently contains
        /// a block with the given identifier.
        /// </returns>
        /// <remarks>
        /// It is discouraged for a call to this method to block indefinitely
        /// while waiting for a block with the given identifier to exist,
        /// rather than return <c>false</c> immediately.
        /// </remarks>
        bool ContainsBlock(FancyByteArray blockIdentifier);

        /// <summary>
        /// Indicates whether this chain store currently contains
        /// a transaction with the given identifier.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to search for.
        /// </param>
        /// <returns>
        /// A value indicating whether this chain store currently contains
        /// a transaction with the given identifier.
        /// </returns>
        /// <remarks>
        /// It is discouraged for a call to this method to block indefinitely
        /// while waiting for a transaction with the given identifier to exist,
        /// rather than return <c>false</c> immediately.
        /// </remarks>
        bool ContainsTransaction(FancyByteArray transactionIdentifier);

        /// <summary>
        /// Indicates whether this chain store currently contains
        /// a block with the given identifier, storing a found block
        /// to an out parameter on success.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to search for.
        /// </param>
        /// <param name="block">
        /// A location to store the block, if it is found.
        /// </param>
        /// <returns>
        /// A value indicating whether this chain store currently contains
        /// a block with the given identifier.
        /// </returns>
        /// <remarks>
        /// It is discouraged for a call to this method to block indefinitely
        /// while waiting for a block with the given identifier to exist,
        /// rather than return <c>false</c> immediately.
        /// </remarks>
        bool TryGetBlock(FancyByteArray blockIdentifier, out IBlock block);

        /// <summary>
        /// Indicates whether this chain store currently contains
        /// a transaction with the given identifier, storing a found transaction
        /// to an out parameter on success.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to search for.
        /// </param>
        /// <param name="transaction">
        /// A location to store the transaction, if it is found.
        /// </param>
        /// <returns>
        /// A value indicating whether this chain store currently contains
        /// a transaction with the given identifier.
        /// </returns>
        /// <remarks>
        /// It is discouraged for a call to this method to block indefinitely
        /// while waiting for a transaction with the given identifier to exist,
        /// rather than return <c>false</c> immediately.
        /// </remarks>
        bool TryGetTransaction(FancyByteArray transactionIdentifier, out ITransaction transaction);

        /// <summary>
        /// Gets a block with the given identifier from this chain store,
        /// asynchronously.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to get.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task<IBlock> GetBlockAsync(FancyByteArray blockIdentifier);

        /// <summary>
        /// Gets a block with the given identifier from this chain store,
        /// asynchronously.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to get.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task<IBlock> GetBlockAsync(FancyByteArray blockIdentifier, CancellationToken token);

        /// <summary>
        /// Gets a transaction with the given identifier from this chain store,
        /// asynchronously.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to get.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task<ITransaction> GetTransactionAsync(FancyByteArray transactionIdentifier);

        /// <summary>
        /// Gets a transaction with the given identifier from this chain store,
        /// asynchronously.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to get.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task<ITransaction> GetTransactionAsync(FancyByteArray transactionIdentifier, CancellationToken token);

        /// <summary>
        /// Indicates whether this chain store currently contains
        /// a block with the given identifier, asynchronously.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to search for.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task<bool> ContainsBlockAsync(FancyByteArray blockIdentifier);

        /// <summary>
        /// Indicates whether this chain store currently contains
        /// a block with the given identifier, asynchronously.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to search for.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task<bool> ContainsBlockAsync(FancyByteArray blockIdentifier, CancellationToken token);

        /// <summary>
        /// Indicates whether this chain store currently contains
        /// a transaction with the given identifier, asynchronously.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to search for.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task<bool> ContainsTransactionAsync(FancyByteArray transactionIdentifier);

        /// <summary>
        /// Indicates whether this chain store currently contains
        /// a transaction with the given identifier, asynchronously.
        /// </summary>
        /// <param name="transactionIdentifier">
        /// The identifier of the transaction to search for.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        Task<bool> ContainsTransactionAsync(FancyByteArray transactionIdentifier, CancellationToken token);
    }
}
