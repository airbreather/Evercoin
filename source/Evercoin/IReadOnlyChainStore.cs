using System.Threading.Tasks;

namespace Evercoin
{
    public interface IReadOnlyChainStore
    {
        IBlock GetBlock(string blockIdentifier);

        ITransaction GetTransaction(string transactionIdentifier);

        bool ContainsBlock(string blockIdentifier);

        bool ContainsTransaction(string transactionIdentifier);

        Task<bool> ContainsBlockAsync(string blockIdentifier);

        Task<bool> ContainsTransactionAsync(string transactionIdentifier);

        bool TryGetBlock(string blockIdentifier, out IBlock block);

        bool TryGetTransaction(string transactionIdentifier, out ITransaction transaction);

        Task<IBlock> GetBlockAsync(string blockIdentifier);

        Task<ITransaction> GetTransactionAsync(string transactionIdentifier);
    }
}
