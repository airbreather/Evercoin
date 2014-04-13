using System.Collections.Generic;

namespace Evercoin
{
    public sealed class ScriptEvaluationResult
    {
        private readonly Stack<FancyByteArray> mainStack;

        private readonly Stack<FancyByteArray> alternateStack;

        public ScriptEvaluationResult(Stack<FancyByteArray> mainStack, Stack<FancyByteArray> alternateStack)
        {
            this.mainStack = mainStack;
            this.alternateStack = alternateStack;
        }

        public static ScriptEvaluationResult False { get { return new ScriptEvaluationResult(new Stack<FancyByteArray>(), new Stack<FancyByteArray>()); } }

        public Stack<FancyByteArray> MainStack { get { return this.mainStack; } }

        public Stack<FancyByteArray> AlternateStack { get { return this.alternateStack; } } 

        public static implicit operator bool(ScriptEvaluationResult result)
        {
            return result.mainStack.Count > 0 &&
                   result.mainStack.Peek();
        }
    }
}
