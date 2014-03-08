using System.Collections.Generic;

namespace Evercoin
{
    /// <summary>
    /// Represents an object that is able to check signatures in script runs.
    /// Expected to also use transaction-specific values.
    /// </summary>
    public interface ISignatureChecker
    {
        bool CheckSignature(IEnumerable<byte> signature, IEnumerable<byte> publicKey, IEnumerable<TransactionScriptOperation> scriptCode);
    }
}
