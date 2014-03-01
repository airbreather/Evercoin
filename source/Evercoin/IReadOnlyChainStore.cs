using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin
{
    public interface IReadOnlyChainStore : IDisposable
    {
        IBlock GetBlock(BigInteger blockIdentifier);

        ITransaction GetTransaction(BigInteger transactionIdentifier);

        bool ContainsBlock(BigInteger blockIdentifier);

        bool ContainsTransaction(BigInteger transactionIdentifier);

        bool TryGetBlock(BigInteger blockIdentifier, out IBlock block);

        bool TryGetTransaction(BigInteger transactionIdentifier, out ITransaction transaction);

        Task<IBlock> GetBlockAsync(BigInteger blockIdentifier);

        Task<IBlock> GetBlockAsync(BigInteger blockIdentifier, CancellationToken token);

        Task<ITransaction> GetTransactionAsync(BigInteger transactionIdentifier);

        Task<ITransaction> GetTransactionAsync(BigInteger transactionIdentifier, CancellationToken token);

        Task<bool> ContainsBlockAsync(BigInteger blockIdentifier);

        Task<bool> ContainsBlockAsync(BigInteger blockIdentifier, CancellationToken token);

        Task<bool> ContainsTransactionAsync(BigInteger transactionIdentifier);

        Task<bool> ContainsTransactionAsync(BigInteger transactionIdentifier, CancellationToken token);
    }
}
