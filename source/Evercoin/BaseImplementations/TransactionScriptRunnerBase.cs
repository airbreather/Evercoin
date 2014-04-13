using System.Collections.Generic;

namespace Evercoin.BaseImplementations
{
    public abstract class TransactionScriptRunnerBase : ITransactionScriptRunner
    {
        public ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker)
        {
            return this.EvaluateScript(scriptOperations, signatureChecker, new Stack<FancyByteArray>());
        }

        public ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker, Stack<FancyByteArray> mainStack)
        {
            return this.EvaluateScript(scriptOperations, signatureChecker, mainStack, new Stack<FancyByteArray>());
        }

        public abstract ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker, Stack<FancyByteArray> mainStack, Stack<FancyByteArray> alternateStack);
    }
}
