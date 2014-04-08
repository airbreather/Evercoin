using System.Collections.Generic;

namespace Evercoin
{
    /// <summary>
    /// Serializes and deserializes blocks and transactions.
    /// </summary>
    public interface IChainSerializer
    {
        FancyByteArray GetBytesForBlock(IBlock block);

        FancyByteArray GetBytesForTransaction(ITransaction transaction);

        IBlock GetBlockForBytes(IEnumerable<byte> serializedBlock);

        ITransaction GetTransactionForBytes(IEnumerable<byte> serializedTransaction);
    }
}
