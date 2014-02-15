using System;
using System.Collections.Generic;
using System.Linq;

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

            Stack<StackItem> mainStack = new Stack<StackItem>();
            Stack<StackItem> alternateStack = new Stack<StackItem>();
            Stack<bool> conditionalStack = new Stack<bool>();
            using (IEnumerator<byte> bytes = serializedScript.GetEnumerator())
            {
                while (bytes.MoveNext())
                {
                    if (!Eval(bytes, mainStack, alternateStack, conditionalStack))
                    {
                        return false;
                    }
                }
            }

            return mainStack.Any() && mainStack.Peek();
        }

        private static bool Eval(IEnumerator<byte> bytes, Stack<StackItem> mainStack, Stack<StackItem> alternateStack, Stack<bool> conditionalStack)
        {
            byte opcodeByte = bytes.Current;
            ScriptOperation opcode = (ScriptOperation)opcodeByte;
            bool actuallyExecute = conditionalStack.All(x => x);

            if (opcode <= ScriptOperation.END_OP_DATA &&
                opcode >= ScriptOperation.BEGIN_OP_DATA)
            {
                byte dataSize = (byte)unchecked(opcodeByte - ScriptOperation.BEGIN_OP_DATA);

                // Next {n} bytes contain the data to push.
                byte[] dataToPush = new byte[dataSize];
                if (!TryReadBytes(dataSize, bytes, dataToPush))
                {
                    return false;
                }

                if (actuallyExecute)
                {
                    StackItem item = new StackItem(dataToPush);
                    mainStack.Push(item);
                }

                return true;
            }

            if (opcode >= ScriptOperation.BEGIN_UNUSED &&
                opcode <= ScriptOperation.END_UNUSED)
            {
                return false;
            }

            // Braces are added to case statements only where the case handler
            // defines a new local variable.
            switch (opcode)
            {
                #region NOOP

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

                #endregion NOOP

                #region Disabled

                // These are all disabled unconditionally.
                case ScriptOperation.OP_CAT:
                case ScriptOperation.OP_SUBSTR:
                case ScriptOperation.OP_LEFT:
                case ScriptOperation.OP_RIGHT:
                case ScriptOperation.OP_INVERT:
                case ScriptOperation.OP_AND:
                case ScriptOperation.OP_OR:
                case ScriptOperation.OP_XOR:
                case ScriptOperation.OP_2MUL:
                case ScriptOperation.OP_2DIV:
                case ScriptOperation.OP_MUL:
                case ScriptOperation.OP_DIV:
                case ScriptOperation.OP_MOD:
                case ScriptOperation.OP_LSHIFT:
                case ScriptOperation.OP_RSHIFT:
                case ScriptOperation.OP_VERIF:
                case ScriptOperation.OP_VERNOTIF:
                    return false;

                // These are all disabled only if they're in an executed branch:
                case ScriptOperation.OP_VER:
                case ScriptOperation.OP_RETURN:
                case ScriptOperation.OP_RESERVED:
                case ScriptOperation.OP_RESERVED1:
                case ScriptOperation.OP_RESERVED2:
                    return !actuallyExecute;

                #endregion Disabled

                #region Push Data

                case ScriptOperation.OP_PUSHDATA1:
                case ScriptOperation.OP_PUSHDATA2:
                case ScriptOperation.OP_PUSHDATA4:
                {
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

                    // Next {1,2,4} bytes tell us the size of the data to push.
                    byte[] b = new byte[count];
                    if (!TryReadBytes(count, bytes, b))
                    {
                        return false;
                    }

                    ulong dataSize = BitConverter.ToUInt64(b, 0);

                    // Next {n} bytes contain the data to push.
                    byte[] dataToPush = new byte[dataSize];
                    if (!TryReadBytes(dataSize, bytes, dataToPush))
                    {
                        return false;
                    }

                    if (actuallyExecute)
                    {
                        StackItem item = new StackItem(dataToPush);
                        mainStack.Push(item);
                    }

                    return true;
                }

                #endregion Push Data

                #region Push Value

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
                    if (actuallyExecute)
                    {
                        int valueToPush = opcode - ScriptOperation.OPCODE_IMMEDIATELY_BEFORE_OP_1;
                        mainStack.Push(new StackItem(valueToPush));
                    }

                    return true;

                #endregion Push Value

                #region Control Flow

                case ScriptOperation.OP_IF:
                case ScriptOperation.OP_NOTIF:
                {
                    if (!actuallyExecute)
                    {
                        conditionalStack.Push(false);
                        return true;
                    }

                    if (mainStack.Count < 1)
                    {
                        return false;
                    }

                    bool conditional = mainStack.Pop() ^
                                       (opcode == ScriptOperation.OP_NOTIF);

                    conditionalStack.Push(conditional);
                    return true;
                }

                case ScriptOperation.OP_ELSE:
                case ScriptOperation.OP_ENDIF:
                {
                    if (conditionalStack.Count < 1)
                    {
                        return false;
                    }

                    bool lastCondition = conditionalStack.Pop();

                    if (opcode == ScriptOperation.OP_ELSE)
                    {
                        conditionalStack.Push(!lastCondition);
                    }

                    return true;
                }

                case ScriptOperation.OP_VERIFY:
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    return mainStack.Count > 0 &&
                           mainStack.Pop();

                #endregion Control Flow

                #region Stack Twiddling

                case ScriptOperation.OP_TOALTSTACK:
                    return !actuallyExecute ||
                           MoveItemFromStackToStack(mainStack, alternateStack);

                case ScriptOperation.OP_FROMALTSTACK:
                    return !actuallyExecute ||
                           MoveItemFromStackToStack(alternateStack, mainStack);

                case ScriptOperation.OP_DROP:
                case ScriptOperation.OP_DUP:
                case ScriptOperation.OP_IFDUP:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 1)
                    {
                        return false;
                    }

                    StackItem item = mainStack.Pop();
                    int timesToPushItem = 0;

                    switch (opcode)
                    {
                        case ScriptOperation.OP_DROP:
                            timesToPushItem = 0;
                            break;

                        case ScriptOperation.OP_DUP:
                            timesToPushItem = 2;
                            break;

                        case ScriptOperation.OP_IFDUP:
                            timesToPushItem++;
                            if (item)
                            {
                                timesToPushItem++;
                            }

                            break;
                    }

                    for (int i = 0; i < timesToPushItem; i++)
                    {
                        mainStack.Push(item);
                    }

                    return true;
                }

                case ScriptOperation.OP_DEPTH:
                    if (actuallyExecute)
                    {
                        mainStack.Push(new StackItem(mainStack.Count));
                    }

                    return true;

                case ScriptOperation.OP_2DROP:
                case ScriptOperation.OP_2DUP:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 2)
                    {
                        return false;
                    }

                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();

                    if (opcode == ScriptOperation.OP_2DUP)
                    {
                        mainStack.Push(secondItem);
                        mainStack.Push(firstItem);
                        mainStack.Push(secondItem);
                        mainStack.Push(firstItem);
                    }

                    return true;
                }

                case ScriptOperation.OP_3DUP:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 3)
                    {
                        return false;
                    }

                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();
                    StackItem thirdItem = mainStack.Pop();

                    mainStack.Push(thirdItem);
                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    mainStack.Push(thirdItem);
                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);

                    return true;
                }

                case ScriptOperation.OP_2OVER:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 4)
                    {
                        return false;
                    }

                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();
                    StackItem thirdItem = mainStack.Pop();
                    StackItem fourthItem = mainStack.Pop();

                    mainStack.Push(fourthItem);
                    mainStack.Push(thirdItem);
                    mainStack.Push(fourthItem);
                    mainStack.Push(thirdItem);
                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);

                    return true;
                }

                case ScriptOperation.OP_2ROT:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 6)
                    {
                        return false;
                    }

                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();
                    StackItem thirdItem = mainStack.Pop();
                    StackItem fourthItem = mainStack.Pop();
                    StackItem fifthItem = mainStack.Pop();
                    StackItem sixthItem = mainStack.Pop();

                    mainStack.Push(fourthItem);
                    mainStack.Push(thirdItem);
                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    mainStack.Push(sixthItem);
                    mainStack.Push(fifthItem);

                    return true;
                }

                case ScriptOperation.OP_2SWAP:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 4)
                    {
                        return false;
                    }

                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();
                    StackItem thirdItem = mainStack.Pop();
                    StackItem fourthItem = mainStack.Pop();

                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    mainStack.Push(fourthItem);
                    mainStack.Push(thirdItem);

                    return true;
                }

                case ScriptOperation.OP_NIP:
                case ScriptOperation.OP_OVER:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 2)
                    {
                        return false;
                    }

                    StackItem innocentBystander = mainStack.Pop();
                    StackItem secondFromTop = mainStack.Pop();
                    mainStack.Push(innocentBystander);

                    if (opcode == ScriptOperation.OP_OVER)
                    {
                        mainStack.Push(secondFromTop);
                    }

                    return true;
                }

                case ScriptOperation.OP_PICK:
                case ScriptOperation.OP_ROLL:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 1)
                    {
                        return false;
                    }

                    ulong fetchDepth = mainStack.Pop();
                    if ((ulong)mainStack.Count <= fetchDepth)
                    {
                        return false;
                    }

                    // we need somewhere to store all the stack items we pop,
                    // so we can push them back afterwards.
                    Stack<StackItem> temporaryStack = new Stack<StackItem>();
                    while (fetchDepth-- > 0)
                    {
                        StackItem nextItem = mainStack.Pop();
                        temporaryStack.Push(nextItem);
                    }

                    StackItem chosenItem = mainStack.Peek();
                    if (opcode == ScriptOperation.OP_ROLL)
                    {
                        mainStack.Pop();
                    }

                    while (temporaryStack.Count > 0)
                    {
                        StackItem nextItem = temporaryStack.Pop();
                        mainStack.Push(nextItem);
                    }

                    mainStack.Push(chosenItem);
                    return true;
                }

                case ScriptOperation.OP_ROT:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 3)
                    {
                        return false;
                    }

                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();
                    StackItem thirdItem = mainStack.Pop();

                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    mainStack.Push(thirdItem);

                    return true;
                }

                case ScriptOperation.OP_SWAP:
                case ScriptOperation.OP_TUCK:
                {
                    if (!actuallyExecute)
                    {
                        return true;
                    }

                    if (mainStack.Count < 2)
                    {
                        return false;
                    }

                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();

                    if (opcode == ScriptOperation.OP_TUCK)
                    {
                        mainStack.Push(firstItem);
                    }

                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    return true;
                }
                    
                #endregion

                default:
                    throw new NotImplementedException("Still working on it!");
            }
        }

        private static bool MoveItemFromStackToStack<T>(Stack<T> sourceStack, Stack<T> destinationStack)
        {
            if (sourceStack.Count < 1)
            {
                return false;
            }

            T item = sourceStack.Pop();
            destinationStack.Push(item);
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
