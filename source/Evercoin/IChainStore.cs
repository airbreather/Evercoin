using System.Threading;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Represents storage for a block / transaction chain.
    /// </summary>
    public interface IChainStore : IReadOnlyChainStore
    {
        void PutBlock(IBlock block);

        void PutTransaction(ITransaction transaction);

        Task PutBlockAsync(IBlock block);

        Task PutBlockAsync(IBlock block, CancellationToken token);

        Task PutTransactionAsync(ITransaction transaction);

        Task PutTransactionAsync(ITransaction transaction, CancellationToken token);
    }
}
