using System.Collections.Generic;

namespace Evercoin
{
    /// <summary>
    /// Runs a script and returns the results of execution.
    /// </summary>
    /// <remarks>
    /// TODO: script runner should depend on script operations, not de-facto
    /// depending on <see cref="ITransactionScriptParser"/>.
    /// </remarks>
    public interface ITransactionScriptRunner
    {
        /// <summary>
        /// Runs a script and returns the results of execution.
        /// </summary>
        /// <param name="scriptOperations">
        /// The serialized script.
        /// </param>
        /// <param name="signatureChecker">
        /// An <see cref="ISignatureChecker"/> to use for validating signatures.
        /// </param>
        /// <returns>
        /// The results of executing the given script.
        /// </returns>
        ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker);

        /// <summary>
        /// Runs a script and returns the results of execution.
        /// </summary>
        /// <param name="scriptOperations">
        /// The serialized script.
        /// </param>
        /// <param name="signatureChecker">
        /// An <see cref="ISignatureChecker"/> to use for validating signatures.
        /// </param>
        /// <param name="mainStack">
        /// The stack to use as a starting point.
        /// </param>
        /// <returns>
        /// The results of executing the given script.
        /// </returns>
        ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker, Stack<StackItem> mainStack);

        /// <summary>
        /// Runs a script and returns the results of execution.
        /// </summary>
        /// <param name="scriptOperations">
        /// The serialized script.
        /// </param>
        /// <param name="signatureChecker">
        /// An <see cref="ISignatureChecker"/> to use for validating signatures.
        /// </param>
        /// <param name="mainStack">
        /// The stack to use as a starting point.
        /// </param>
        /// <param name="alternateStack">
        /// The alternate stack to use as a starting point.
        /// </param>
        /// <returns>
        /// The results of executing the given script.
        /// </returns>
        ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker, Stack<StackItem> mainStack, Stack<StackItem> alternateStack);
    }
}
