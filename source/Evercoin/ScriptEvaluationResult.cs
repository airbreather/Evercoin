using System.Collections.Generic;

namespace Evercoin
{
    public sealed class ScriptEvaluationResult
    {
        public static readonly ScriptEvaluationResult False = new ScriptEvaluationResult(new Stack<StackItem>(), new Stack<StackItem>());

        private readonly Stack<StackItem> mainStack;

        private readonly Stack<StackItem> alternateStack;

        public ScriptEvaluationResult(Stack<StackItem> mainStack, Stack<StackItem> alternateStack)
        {
            this.mainStack = mainStack;
            this.alternateStack = alternateStack;
        }

        public Stack<StackItem> MainStack { get { return this.mainStack; } }

        public Stack<StackItem> AlternateStack { get { return this.alternateStack; } } 

        public static implicit operator bool(ScriptEvaluationResult result)
        {
            return result.mainStack.Count > 0 &&
                   result.mainStack.Peek();
        }
    }
}
