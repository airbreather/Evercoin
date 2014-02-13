using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptRunner : ITransactionScriptRunner
    {
        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public TransactionScriptRunner(IHashAlgorithmStore hashAlgorithmStore)
        {
            if (hashAlgorithmStore == null)
            {
                throw new ArgumentNullException("hashAlgorithmStore");
            }

            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public bool EvaluateTransactionScript(IEnumerable<byte> serializedScript)
        {
            if (serializedScript == null)
            {
                throw new ArgumentNullException("serializedScript");
            }

            Stack<StackItem> stack = new Stack<StackItem>();
            Stack<StackItem> alternateStack = new Stack<StackItem>();
            Stack<bool> conditionalStack = new Stack<bool>();
            using (IEnumerator<byte> bytes = serializedScript.GetEnumerator())
            {
                while (bytes.MoveNext())
                {
                    if (!Eval(bytes, stack, alternateStack, conditionalStack))
                    {
                        return false;
                    }
                }
            }

            return stack.Any() && stack.Peek();
        }

        private static bool Eval(IEnumerator<byte> bytes, Stack<StackItem> stack, Stack<StackItem> alternateStack, Stack<bool> conditionalStack)
        {
            byte opcodeByte = bytes.Current;
            ScriptOperation opcode = (ScriptOperation)opcodeByte;
            if (ScriptOperation.BEGIN_OP_DATA <= opcode &&
                opcode >= ScriptOperation.END_OP_DATA)
            {
                return PushData(opcodeByte, bytes, stack);
            }

            if (opcode >= ScriptOperation.BEGIN_UNUSED &&
                opcode <= ScriptOperation.END_UNUSED)
            {
                return false;
            }

            switch (opcode)
            {
                case ScriptOperation.OP_PUSHDATA1:
                case ScriptOperation.OP_PUSHDATA2:
                case ScriptOperation.OP_PUSHDATA4:
                    byte count = 1;
                    switch (opcode)
                    {
                        case ScriptOperation.OP_PUSHDATA2:
                            count = 2;
                            break;
                        case ScriptOperation.OP_PUSHDATA4:
                            count = 4;
                            break;
                    }

                    byte[] b = new byte[count];
                    if (!TryReadBytes(count, bytes, b))
                    {
                        return false;
                    }

                    ulong num = BitConverter.ToUInt64(b, 0);
                    return PushData(num, bytes, stack);

                case ScriptOperation.OP_1NEGATE:
                case ScriptOperation.OP_1:
                case ScriptOperation.OP_2:
                case ScriptOperation.OP_3:
                case ScriptOperation.OP_4:
                case ScriptOperation.OP_5:
                case ScriptOperation.OP_6:
                case ScriptOperation.OP_7:
                case ScriptOperation.OP_8:
                case ScriptOperation.OP_9:
                case ScriptOperation.OP_10:
                case ScriptOperation.OP_11:
                case ScriptOperation.OP_12:
                case ScriptOperation.OP_13:
                case ScriptOperation.OP_14:
                case ScriptOperation.OP_15:
                case ScriptOperation.OP_16:
                    int numberToPush = (int)opcode - ((int)ScriptOperation.OP_1 - 1);
                    stack.Push(new StackItem(numberToPush));
                    return true;

                case ScriptOperation.OP_NOP:
                case ScriptOperation.OP_NOP1:
                case ScriptOperation.OP_NOP2:
                case ScriptOperation.OP_NOP3:
                case ScriptOperation.OP_NOP4:
                case ScriptOperation.OP_NOP5:
                case ScriptOperation.OP_NOP6:
                case ScriptOperation.OP_NOP7:
                case ScriptOperation.OP_NOP8:
                case ScriptOperation.OP_NOP9:
                case ScriptOperation.OP_NOP10:
                    return true;

                default:
                    throw new NotImplementedException("Still working on it!");
            }
        }

        private static bool PushData(ulong count, IEnumerator<byte> bytes, Stack<StackItem> stack)
        {
            byte[] dataToPush = new byte[count];
            if (!TryReadBytes(count, bytes, dataToPush))
            {
                return false;
            }

            stack.Push(new StackItem(dataToPush));
            return true;
        }

        private static bool TryReadBytes(ulong count, IEnumerator<byte> bytes, byte[] value)
        {
            ulong currentIndex = 0;

            while (count-- > 0)
            {
                if (!bytes.MoveNext())
                {
                    return false;
                }

                value[currentIndex++] = bytes.Current;
            }

            return true;
        }
    }
}
