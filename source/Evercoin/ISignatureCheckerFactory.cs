namespace Evercoin
{
    /// <summary>
    /// Represents a factory that can create <see cref="ISignatureChecker"/>
    /// objects that validate signatures for a given transaction, from the
    /// perspective of a given one of its outputs.
    /// </summary>
    public interface ISignatureCheckerFactory
    {
        /// <summary>
        /// Creates an <see cref="ISignatureChecker"/> object that can check
        /// signatures against a given transaction, from the perspective
        /// of a given one of its outputs.
        /// </summary>
        /// <param name="transaction">
        /// The transaction to use for validation.
        /// </param>
        /// <param name="outputIndex">
        /// The index of the output in the given transaction's output collection
        /// to use as the perspective from which to validate the signatures.
        /// </param>
        /// <returns>
        /// An <see cref="ISignatureChecker"/> object that can check
        /// signatures against a given transaction, from the perspective
        /// of a given one of its outputs.
        /// </returns>
        ISignatureChecker CreateSignatureChecker(ITransaction transaction, int outputIndex);
    }
}
