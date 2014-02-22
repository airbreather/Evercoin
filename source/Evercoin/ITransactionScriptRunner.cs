using System.Collections.Generic;

namespace Evercoin
{
    public interface ITransactionScriptRunner
    {
        bool EvaluateScript(IEnumerable<byte> serializedScript, ISignatureChecker signatureChecker);
    }
}
