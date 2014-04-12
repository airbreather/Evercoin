using System.Collections.Generic;

namespace Evercoin
{
    public interface IBlockChain
    {
        ulong BlockCount { get; }

        FancyByteArray? GetIdentifierOfBlockAtHeight(ulong height);

        FancyByteArray? GetIdentifierOfBlockWithTransaction(FancyByteArray transactionIdentifier);

        ulong? GetHeightOfBlock(FancyByteArray blockIdentifier);

        void AddBlockAtHeight(FancyByteArray block, ulong height);

        void AddTransactionToBlock(FancyByteArray transactionIdentifier, FancyByteArray blockIdentifier, ulong index);

        IEnumerable<FancyByteArray> GetTransactionsForBlock(FancyByteArray blockIdentifier);

        void RemoveBlocksAboveHeight(ulong height);
    }
}
