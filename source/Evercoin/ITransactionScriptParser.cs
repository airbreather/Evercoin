using System.Collections.Generic;

namespace Evercoin
{
    public interface ITransactionScriptParser
    {
        TransactionScriptOperation[] Parse(IEnumerable<byte> bytes);
    }
}
