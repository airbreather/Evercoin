using System.Collections.Generic;

namespace Evercoin.BaseImplementations
{
    public abstract class TransactionScriptRunnerBase : ITransactionScriptRunner
    {
        public ScriptEvaluationResult EvaluateScript(IEnumerable<byte> serializedScript, ISignatureChecker signatureChecker)
        {
            return this.EvaluateScript(serializedScript, signatureChecker, new Stack<StackItem>());
        }

        public ScriptEvaluationResult EvaluateScript(IEnumerable<byte> serializedScript, ISignatureChecker signatureChecker, Stack<StackItem> mainStack)
        {
            return this.EvaluateScript(serializedScript, signatureChecker, mainStack, new Stack<StackItem>());
        }

        public abstract ScriptEvaluationResult EvaluateScript(IEnumerable<byte> serializedScript, ISignatureChecker signatureChecker, Stack<StackItem> mainStack, Stack<StackItem> alternateStack);
    }
}
