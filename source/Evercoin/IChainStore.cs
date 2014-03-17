using System.Numerics;
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
    public interface IChainStore : IReadOnlyChainStore
    {
        void PutBlock(BigInteger blockIdentifier, IBlock block);

        void PutTransaction(BigInteger transactionIdentifier, ITransaction transaction);

        Task PutBlockAsync(BigInteger blockIdentifier, IBlock block);

        Task PutBlockAsync(BigInteger blockIdentifier, IBlock block, CancellationToken token);

        Task PutTransactionAsync(BigInteger transactionIdentifier, ITransaction transaction);

        Task PutTransactionAsync(BigInteger transactionIdentifier, ITransaction transaction, CancellationToken token);
    }
}
