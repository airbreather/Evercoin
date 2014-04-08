using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Evercoin
{
    /// <summary>
    /// Parses a sequence of bytes into a sequence of script operations.
    /// </summary>
    public interface ITransactionScriptParser
    {
        /// <summary>
        /// Parses a sequence of bytes into a sequence of script operations.
        /// </summary>
        /// <param name="bytes">
        /// The bytes that store the serialized script.
        /// </param>
        /// <returns>
        /// The sequence of script operations that represent the given bytes.
        /// </returns>
        ReadOnlyCollection<TransactionScriptOperation> Parse(IEnumerable<byte> bytes);
    }
}
