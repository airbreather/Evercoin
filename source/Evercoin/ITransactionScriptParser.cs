using System.Collections.Generic;
using System.Collections.Immutable;

namespace Evercoin
{
    public interface ITransactionScriptParser
    {
        IEnumerable<TransactionScriptOperation> Parse(IEnumerable<byte> bytes);
    }
}
