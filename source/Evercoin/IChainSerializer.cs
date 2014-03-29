using System.Collections.Generic;

namespace Evercoin
{
    /// <summary>
    /// Serializes and deserializes blocks and transactions.
    /// </summary>
    public interface IChainSerializer
    {
        byte[] GetBytesForBlock(IBlock block);

        byte[] GetBytesForTransaction(ITransaction transaction);

        IBlock GetBlockForBytes(IEnumerable<byte> serializedBlock);

        ITransaction GetTransactionForBytes(IEnumerable<byte> serializedTransaction);
    }
}
