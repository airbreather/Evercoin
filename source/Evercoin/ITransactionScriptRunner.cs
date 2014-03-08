using System.Collections.Generic;

namespace Evercoin
{
    public interface ITransactionScriptRunner
    {
        ScriptEvaluationResult EvaluateScript(IEnumerable<byte> serializedScript, ISignatureChecker signatureChecker);

        ScriptEvaluationResult EvaluateScript(IEnumerable<byte> serializedScript, ISignatureChecker signatureChecker, Stack<StackItem> mainStack);

        ScriptEvaluationResult EvaluateScript(IEnumerable<byte> serializedScript, ISignatureChecker signatureChecker, Stack<StackItem> mainStack, Stack<StackItem> alternateStack);
    }
}
