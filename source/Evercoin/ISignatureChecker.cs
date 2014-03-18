using System.Collections.Generic;

namespace Evercoin
{
    /// <summary>
    /// Represents an object that is able to check signatures in script runs.
    /// Expected to also use transaction-specific values.
    /// </summary>
    public interface ISignatureChecker
    {
        /// <summary>
        /// Checks that a given signature is valid for the given transaction,
        /// according to the given script.
        /// </summary>
        /// <param name="signature">
        /// The signature to check.
        /// </param>
        /// <param name="publicKey">
        /// The public key to check against the signature.
        /// </param>
        /// <param name="scriptCode">
        /// The script to check.
        /// </param>
        /// <returns>
        /// A value indicating whether or not the signature is valid.
        /// </returns>
        /// <remarks>
        /// TODO: Improper separation of responsibilities between this method
        /// and its caller.  Need to have either more or less in this method.
        /// </remarks>
        bool CheckSignature(IEnumerable<byte> signature, IEnumerable<byte> publicKey, IEnumerable<TransactionScriptOperation> scriptCode);
    }
}
