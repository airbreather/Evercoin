using System.Collections.Generic;

namespace Evercoin.BaseImplementations
{
    public abstract class TransactionScriptRunnerBase : ITransactionScriptRunner
    {
        public ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker)
        {
            return this.EvaluateScript(scriptOperations, signatureChecker, new Stack<StackItem>());
        }

        public ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker, Stack<StackItem> mainStack)
        {
            return this.EvaluateScript(scriptOperations, signatureChecker, mainStack, new Stack<StackItem>());
        }

        public abstract ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker, Stack<StackItem> mainStack, Stack<StackItem> alternateStack);
    }
}
