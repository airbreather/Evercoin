using System.Threading;
using System.Threading.Tasks;

namespace Evercoin
{
    public interface IReadOnlyChainStore
    {
        IBlock GetBlock(string blockIdentifier);

        ITransaction GetTransaction(string transactionIdentifier);

        bool ContainsBlock(string blockIdentifier);

        bool ContainsTransaction(string transactionIdentifier);

        bool TryGetBlock(string blockIdentifier, out IBlock block);

        bool TryGetTransaction(string transactionIdentifier, out ITransaction transaction);

        Task<IBlock> GetBlockAsync(string blockIdentifier);

        Task<IBlock> GetBlockAsync(string blockIdentifier, CancellationToken token);

        Task<ITransaction> GetTransactionAsync(string transactionIdentifier);

        Task<ITransaction> GetTransactionAsync(string transactionIdentifier, CancellationToken token);

        Task<bool> ContainsBlockAsync(string blockIdentifier);

        Task<bool> ContainsBlockAsync(string blockIdentifier, CancellationToken token);

        Task<bool> ContainsTransactionAsync(string transactionIdentifier);

        Task<bool> ContainsTransactionAsync(string transactionIdentifier, CancellationToken token);
    }
}
