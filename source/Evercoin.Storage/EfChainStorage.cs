using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;

namespace Evercoin.Storage
{
    [Export("UncachedChainStore", typeof(IChainStore))]
    public sealed class EfChainStorage : ReadWriteChainStoreBase
    {
        private readonly Bitcoin dbContext = new Bitcoin();

        protected override IBlock FindBlockCore(string blockIdentifier)
        {
            return this.dbContext.Blocks.Find(blockIdentifier);
        }

        protected override ITransaction FindTransactionCore(string transactionIdentifier)
        {
            return this.dbContext.Transactions.Find(transactionIdentifier);
        }

        protected override async Task<IBlock> FindBlockAsyncCore(string blockIdentifier, CancellationToken token)
        {
            return await this.dbContext.Blocks.FindAsync(token, blockIdentifier);
        }

        protected override async Task<ITransaction> FindTransactionAsyncCore(string transactionIdentifier, CancellationToken token)
        {
            return await this.dbContext.Transactions.FindAsync(token, transactionIdentifier);
        }

        protected override void PutBlockCore(IBlock block)
        {
            this.dbContext.Blocks.Add(new Block(block));
            this.dbContext.SaveChanges();
        }

        protected override void PutTransactionCore(ITransaction transaction)
        {
            this.dbContext.Transactions.Add(new Transaction(transaction));
            this.dbContext.SaveChanges();
        }

        protected override async Task PutBlockAsyncCore(IBlock block, CancellationToken token)
        {
            this.dbContext.Blocks.Add(new Block(block));
            await this.dbContext.SaveChangesAsync(token);
        }

        protected override async Task PutTransactionAsyncCore(ITransaction transaction, CancellationToken token)
        {
            this.dbContext.Transactions.Add(new Transaction(transaction));
            await this.dbContext.SaveChangesAsync(token);
        }

        protected override void DisposeManagedResources()
        {
            this.dbContext.Dispose();
        }
    }
}
