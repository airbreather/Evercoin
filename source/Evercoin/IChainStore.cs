using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Represents storage for a block / transaction chain.
    /// </summary>
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
