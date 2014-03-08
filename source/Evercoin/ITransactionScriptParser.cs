using System.Collections.Generic;
using System.Collections.Immutable;

namespace Evercoin
{
    public interface ITransactionScriptParser
    {
        ImmutableList<TransactionScriptOperation> Parse(IEnumerable<byte> bytes);
    }
}
