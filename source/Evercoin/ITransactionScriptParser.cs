using System.Collections.Generic;

namespace Evercoin
{
    public interface ITransactionScriptParser
    {
        IEnumerable<TransactionScriptOperation> Parse(IEnumerable<byte> bytes);
    }
}
