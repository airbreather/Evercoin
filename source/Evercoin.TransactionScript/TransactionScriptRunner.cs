using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;

using Evercoin.Util;

namespace Evercoin.TransactionScript
{
    [Export(typeof(ITransactionScriptRunner))]
    public sealed class TransactionScriptRunner : ITransactionScriptRunner
    {
        #region Various Definitions

        /// <summary>
        /// The set of opcodes that we can just completely skip on,
        /// if we're in an unexecuted conditional branch.
        /// </summary>
        private static readonly HashSet<ScriptOperation> RealOpcodesWithoutData = new HashSet<ScriptOperation>
                                                                                  {
                                                                                      ScriptOperation.OP_VER,
                                                                                      ScriptOperation.OP_RETURN,
                                                                                      ScriptOperation.OP_RESERVED,
                                                                                      ScriptOperation.OP_RESERVED1,
                                                                                      ScriptOperation.OP_RESERVED2,
                                                                                      ScriptOperation.OP_1NEGATE,
                                                                                      ScriptOperation.OP_1,
                                                                                      ScriptOperation.OP_2,
                                                                                      ScriptOperation.OP_3,
                                                                                      ScriptOperation.OP_4,
                                                                                      ScriptOperation.OP_5,
                                                                                      ScriptOperation.OP_6,
                                                                                      ScriptOperation.OP_7,
                                                                                      ScriptOperation.OP_8,
                                                                                      ScriptOperation.OP_9,
                                                                                      ScriptOperation.OP_10,
                                                                                      ScriptOperation.OP_11,
                                                                                      ScriptOperation.OP_12,
                                                                                      ScriptOperation.OP_13,
                                                                                      ScriptOperation.OP_14,
                                                                                      ScriptOperation.OP_15,
                                                                                      ScriptOperation.OP_16,
                                                                                      ScriptOperation.OP_VERIFY,
                                                                                      ScriptOperation.OP_TOALTSTACK,
                                                                                      ScriptOperation.OP_FROMALTSTACK,
                                                                                      ScriptOperation.OP_DROP,
                                                                                      ScriptOperation.OP_DUP,
                                                                                      ScriptOperation.OP_IFDUP,
                                                                                      ScriptOperation.OP_DEPTH,
                                                                                      ScriptOperation.OP_2DROP,
                                                                                      ScriptOperation.OP_2DUP,
                                                                                      ScriptOperation.OP_3DUP,
                                                                                      ScriptOperation.OP_2OVER,
                                                                                      ScriptOperation.OP_2ROT,
                                                                                      ScriptOperation.OP_2SWAP,
                                                                                      ScriptOperation.OP_NIP,
                                                                                      ScriptOperation.OP_OVER,
                                                                                      ScriptOperation.OP_PICK,
                                                                                      ScriptOperation.OP_ROLL,
                                                                                      ScriptOperation.OP_ROT,
                                                                                      ScriptOperation.OP_SWAP,
                                                                                      ScriptOperation.OP_TUCK,
                                                                                      ScriptOperation.OP_NOP,
                                                                                      ScriptOperation.OP_NOP1,
                                                                                      ScriptOperation.OP_NOP2,
                                                                                      ScriptOperation.OP_NOP3,
                                                                                      ScriptOperation.OP_NOP4,
                                                                                      ScriptOperation.OP_NOP5,
                                                                                      ScriptOperation.OP_NOP6,
                                                                                      ScriptOperation.OP_NOP7,
                                                                                      ScriptOperation.OP_NOP8,
                                                                                      ScriptOperation.OP_NOP9,
                                                                                      ScriptOperation.OP_NOP10,
                                                                                      ScriptOperation.OP_SIZE,
                                                                                      ScriptOperation.OP_EQUAL,
                                                                                      ScriptOperation.OP_EQUALVERIFY,
                                                                                      ScriptOperation.OP_1ADD,                
                                                                                      ScriptOperation.OP_1SUB,
                                                                                      ScriptOperation.OP_NEGATE,
                                                                                      ScriptOperation.OP_ABS,
                                                                                      ScriptOperation.OP_NOT,
                                                                                      ScriptOperation.OP_0NOTEQUAL,
                                                                                      ScriptOperation.OP_ADD,
                                                                                      ScriptOperation.OP_SUB,
                                                                                      ScriptOperation.OP_BOOLAND,
                                                                                      ScriptOperation.OP_BOOLOR,
                                                                                      ScriptOperation.OP_NUMEQUAL,
                                                                                      ScriptOperation.OP_NUMEQUALVERIFY,
                                                                                      ScriptOperation.OP_NUMNOTEQUAL,
                                                                                      ScriptOperation.OP_LESSTHAN,
                                                                                      ScriptOperation.OP_GREATERTHAN,
                                                                                      ScriptOperation.OP_LESSTHANOREQUAL,
                                                                                      ScriptOperation.OP_GREATERTHANOREQUAL,
                                                                                      ScriptOperation.OP_MIN,
                                                                                      ScriptOperation.OP_MAX,
                                                                                      ScriptOperation.OP_WITHIN,
                                                                                      ScriptOperation.OP_RIPEMD160,
                                                                                      ScriptOperation.OP_SHA1,
                                                                                      ScriptOperation.OP_SHA256,
                                                                                      ScriptOperation.OP_HASH160,
                                                                                      ScriptOperation.OP_HASH256,
                                                                                      ScriptOperation.OP_CODESEPARATOR,
                                                                                      ScriptOperation.OP_CHECKSIG,
                                                                                      ScriptOperation.OP_CHECKSIGVERIFY,
                                                                                      ScriptOperation.OP_CHECKMULTISIG,
                                                                                      ScriptOperation.OP_CHECKMULTISIGVERIFY,
                                                                                  };

        /// <summary>
        /// The number of items on the stack required to perform an operation.
        /// </summary>
        private static readonly Dictionary<ScriptOperation, int> MinimumRequiredStackDepthForOperation = new Dictionary<ScriptOperation, int>
                                                                                                           {
                                                                                                                { ScriptOperation.OP_IF, 1 },
                                                                                                                { ScriptOperation.OP_NOTIF, 1 },
                                                                                                                { ScriptOperation.OP_VERIFY, 1 },
                                                                                                                { ScriptOperation.OP_DROP, 1 },
                                                                                                                { ScriptOperation.OP_DUP, 1 },
                                                                                                                { ScriptOperation.OP_IFDUP, 1 },
                                                                                                                { ScriptOperation.OP_2DROP, 2 },
                                                                                                                { ScriptOperation.OP_2DUP, 2 },
                                                                                                                { ScriptOperation.OP_3DUP, 3 },
                                                                                                                { ScriptOperation.OP_2OVER, 4 },
                                                                                                                { ScriptOperation.OP_2ROT, 6 },
                                                                                                                { ScriptOperation.OP_2SWAP, 4 },
                                                                                                                { ScriptOperation.OP_NIP, 2 },
                                                                                                                { ScriptOperation.OP_OVER, 2 },
                                                                                                                { ScriptOperation.OP_PICK, 1 },
                                                                                                                { ScriptOperation.OP_ROLL, 1 },
                                                                                                                { ScriptOperation.OP_ROT, 3 },
                                                                                                                { ScriptOperation.OP_SWAP, 2 },
                                                                                                                { ScriptOperation.OP_TUCK, 2 },
                                                                                                                { ScriptOperation.OP_SIZE, 1 },
                                                                                                                { ScriptOperation.OP_EQUAL, 2 },
                                                                                                                { ScriptOperation.OP_EQUALVERIFY, 2 },
                                                                                                                { ScriptOperation.OP_1ADD, 1 },
                                                                                                                { ScriptOperation.OP_1SUB, 1 },
                                                                                                                { ScriptOperation.OP_NEGATE, 1 },
                                                                                                                { ScriptOperation.OP_ABS, 1 },
                                                                                                                { ScriptOperation.OP_NOT, 1 },
                                                                                                                { ScriptOperation.OP_0NOTEQUAL, 1 },
                                                                                                                { ScriptOperation.OP_ADD, 2 },
                                                                                                                { ScriptOperation.OP_SUB, 2 },
                                                                                                                { ScriptOperation.OP_BOOLAND, 2 },
                                                                                                                { ScriptOperation.OP_BOOLOR, 2 },
                                                                                                                { ScriptOperation.OP_NUMEQUAL, 2 },
                                                                                                                { ScriptOperation.OP_NUMEQUALVERIFY, 2 },
                                                                                                                { ScriptOperation.OP_NUMNOTEQUAL, 2 },
                                                                                                                { ScriptOperation.OP_LESSTHAN, 2 },
                                                                                                                { ScriptOperation.OP_GREATERTHAN, 2 },
                                                                                                                { ScriptOperation.OP_LESSTHANOREQUAL, 2 },
                                                                                                                { ScriptOperation.OP_GREATERTHANOREQUAL, 2 },
                                                                                                                { ScriptOperation.OP_MIN, 2 },
                                                                                                                { ScriptOperation.OP_MAX, 2 },
                                                                                                                { ScriptOperation.OP_WITHIN, 3 },
                                                                                                                { ScriptOperation.OP_RIPEMD160, 1 },
                                                                                                                { ScriptOperation.OP_SHA1, 1 },
                                                                                                                { ScriptOperation.OP_SHA256, 1 },
                                                                                                                { ScriptOperation.OP_HASH160, 1 },
                                                                                                                { ScriptOperation.OP_HASH256, 1 },
                                                                                                                { ScriptOperation.OP_CHECKSIG, 2 },
                                                                                                                { ScriptOperation.OP_CHECKSIGVERIFY, 2 },
                                                                                                                { ScriptOperation.OP_CHECKMULTISIG, 1 },
                                                                                                                { ScriptOperation.OP_CHECKMULTISIGVERIFY, 1 },
                                                                                                           };

        #endregion Various Definitions

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        [ImportingConstructor]
        public TransactionScriptRunner(IHashAlgorithmStore hashAlgorithmStore)
        {
            if (hashAlgorithmStore == null)
            {
                throw new ArgumentNullException("hashAlgorithmStore");
            }

            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public bool EvaluateScript(IEnumerable<byte> serializedScript, ISignatureChecker signatureChecker)
        {
            if (serializedScript == null)
            {
                throw new ArgumentNullException("serializedScript");
            }

            if (signatureChecker == null)
            {
                throw new ArgumentNullException("signatureChecker");
            }

            Stack<StackItem> mainStack = new Stack<StackItem>();
            Stack<StackItem> alternateStack = new Stack<StackItem>();
            Stack<bool> conditionalStack = new Stack<bool>();
            using (ScriptEnumerator bytes = new ScriptEnumerator(serializedScript.GetEnumerator()))
            {
                while (bytes.MoveNext())
                {
                    if (!this.Eval(bytes, mainStack, alternateStack, conditionalStack, signatureChecker))
                    {
                        return false;
                    }
                }
            }

            return mainStack.Any() && mainStack.Peek();
        }

        private bool Eval(ScriptEnumerator bytes, Stack<StackItem> mainStack, Stack<StackItem> alternateStack, Stack<bool> conditionalStack, ISignatureChecker signatureChecker)
        {
            byte opcodeByte = bytes.Current;
            ScriptOperation opcode = (ScriptOperation)opcodeByte;
            bool actuallyExecute = conditionalStack.All(x => x);

            if (!actuallyExecute &&
                RealOpcodesWithoutData.Contains(opcode))
            {
                return true;
            }

            int minStackDepth;
            if (!MinimumRequiredStackDepthForOperation.TryGetValue(opcode, out minStackDepth))
            {
                minStackDepth = 0;
            }

            if (mainStack.Count < minStackDepth)
            {
                return false;
            }

            if (opcode <= ScriptOperation.END_OP_DATA &&
                opcode >= ScriptOperation.BEGIN_OP_DATA)
            {
                byte dataSize = (byte)unchecked(opcodeByte - ScriptOperation.BEGIN_OP_DATA);

                // Next {n} bytes contain the data to push.
                byte[] dataToPush = new byte[dataSize];
                if (!TryReadBytes(bytes, dataToPush))
                {
                    return false;
                }

                if (actuallyExecute)
                {
                    mainStack.Push(dataToPush);
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

                // These are all disabled if they're in an executed branch:
                case ScriptOperation.OP_VER:
                case ScriptOperation.OP_RETURN:
                case ScriptOperation.OP_RESERVED:
                case ScriptOperation.OP_RESERVED1:
                case ScriptOperation.OP_RESERVED2:
                    return false;

                #endregion Disabled

                #region Push Data

                case ScriptOperation.OP_PUSHDATA1:
                case ScriptOperation.OP_PUSHDATA2:
                case ScriptOperation.OP_PUSHDATA4:
                {
                    byte[] dataSizeBytes = null;
                    Func<uint> getDataSize = null;
                    switch (opcode)
                    {
                        case ScriptOperation.OP_PUSHDATA1:
                            dataSizeBytes = new byte[1];
                            getDataSize = () => dataSizeBytes[0];
                            break;

                        case ScriptOperation.OP_PUSHDATA2:
                            dataSizeBytes = new byte[2];
                            getDataSize = () => BitConverter.ToUInt16(dataSizeBytes, 0);
                            break;

                        case ScriptOperation.OP_PUSHDATA4:
                            dataSizeBytes = new byte[4];
                            getDataSize = () => BitConverter.ToUInt32(dataSizeBytes, 0);
                            break;
                    }

                    if (!TryReadBytes(bytes, dataSizeBytes))
                    {
                        return false;
                    }

                    // Our data is little-endian, so prepare it for BitConverter usage.
                    dataSizeBytes.LittleEndianToOrFromBitConverterEndianness();

                    uint dataSize = getDataSize();
                    byte[] dataToPush = new byte[dataSize];
                    if (!TryReadBytes(bytes, dataToPush))
                    {
                        return false;
                    }

                    if (actuallyExecute)
                    {
                        mainStack.Push(dataToPush);
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
                {
                    BigInteger valueToPush = opcode - ScriptOperation.OPCODE_IMMEDIATELY_BEFORE_OP_1;
                    mainStack.Push(valueToPush);
                    return true;
                }

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

                #endregion Control Flow

                #region Stack Twiddling

                case ScriptOperation.OP_TOALTSTACK:
                    return MoveItemFromStackToStack(mainStack, alternateStack);

                case ScriptOperation.OP_FROMALTSTACK:
                    return MoveItemFromStackToStack(alternateStack, mainStack);

                case ScriptOperation.OP_DROP:
                    mainStack.Pop();
                    return true;

                case ScriptOperation.OP_DUP:
                case ScriptOperation.OP_IFDUP:
                {
                    StackItem item = mainStack.Peek();
                    if (opcode == ScriptOperation.OP_DUP || item)
                    {
                        mainStack.Push(item);
                    }

                    return true;
                }

                case ScriptOperation.OP_DEPTH:
                {
                    BigInteger depth = mainStack.Count;
                    mainStack.Push(depth);
                    return true;
                }

                case ScriptOperation.OP_SIZE:
                {
                    byte[] item = mainStack.Peek();
                    BigInteger size = item.Length;

                    mainStack.Push(size);
                    return true;
                }

                case ScriptOperation.OP_2DROP:
                case ScriptOperation.OP_2DUP:
                {
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
                    BigInteger fetchDepth = mainStack.Pop();
                    if (mainStack.Count <= fetchDepth)
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

                #region Boolean

                case ScriptOperation.OP_EQUAL:
                case ScriptOperation.OP_EQUALVERIFY:
                {
                    byte[] firstItem = mainStack.Pop();
                    byte[] secondItem = mainStack.Pop();

                    mainStack.Push(firstItem.SequenceEqual(secondItem));

                    return opcode == ScriptOperation.OP_EQUAL ||
                           mainStack.Pop();
                }

                case ScriptOperation.OP_VERIFY:
                    return mainStack.Pop();

                #endregion Boolean

                #region Arithmetic

                case ScriptOperation.OP_1ADD:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(item + 1);
                    return true;
                }

                case ScriptOperation.OP_1SUB:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(item - 1);
                    return true;
                }

                case ScriptOperation.OP_NEGATE:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(-item);
                    return true;
                }

                case ScriptOperation.OP_ABS:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(item.Sign < 0 ? -item : item);
                    return true;
                }

                case ScriptOperation.OP_NOT:
                {
                    bool value = mainStack.Pop();
                    mainStack.Push(!value);
                    return true;
                }

                case ScriptOperation.OP_0NOTEQUAL:
                {
                    bool value = mainStack.Pop();
                    mainStack.Push(value);
                    return true;
                }

                case ScriptOperation.OP_ADD:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue + secondValue);
                    return true;
                }

                case ScriptOperation.OP_SUB:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue - secondValue);
                    return true;
                }

                case ScriptOperation.OP_BOOLAND:
                {
                    bool firstValue = mainStack.Pop();
                    bool secondValue = mainStack.Pop();
                    mainStack.Push(firstValue && secondValue);
                    return true;
                }

                case ScriptOperation.OP_BOOLOR:
                {
                    bool firstValue = mainStack.Pop();
                    bool secondValue = mainStack.Pop();
                    mainStack.Push(firstValue || secondValue);
                    return true;
                }

                case ScriptOperation.OP_NUMEQUAL:
                case ScriptOperation.OP_NUMEQUALVERIFY:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue == secondValue);
                    return true;
                }

                case ScriptOperation.OP_NUMNOTEQUAL:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue != secondValue);
                    return true;
                }

                case ScriptOperation.OP_LESSTHAN:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue < secondValue);
                    return true;
                }

                case ScriptOperation.OP_GREATERTHAN:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue > secondValue);
                    return true;
                }

                case ScriptOperation.OP_LESSTHANOREQUAL:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue <= secondValue);
                    return true;
                }

                case ScriptOperation.OP_GREATERTHANOREQUAL:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue >= secondValue);
                    return true;
                }

                case ScriptOperation.OP_MIN:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(BigInteger.Min(firstValue, secondValue));
                    return true;
                }

                case ScriptOperation.OP_MAX:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(BigInteger.Max(firstValue, secondValue));
                    return true;
                }

                case ScriptOperation.OP_WITHIN:
                {
                    BigInteger maxValue = mainStack.Pop();
                    BigInteger minValue = mainStack.Pop();
                    BigInteger valueToCompare = mainStack.Pop();

                    mainStack.Push(minValue <= valueToCompare &&
                                   valueToCompare < maxValue);

                    return true;
                }

                #endregion Arithmetic

                #region Crypto

                case ScriptOperation.OP_RIPEMD160:
                case ScriptOperation.OP_SHA1:
                case ScriptOperation.OP_SHA256:
                case ScriptOperation.OP_HASH160:
                case ScriptOperation.OP_HASH256:
                {

                    Guid hashAlgorithmIdentifier = Guid.Empty;
                    switch (opcode)
                    {   
                        case ScriptOperation.OP_RIPEMD160:
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.RipeMd160;
                            break;
                        case ScriptOperation.OP_SHA1:
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.SHA1;
                            break;
                        case ScriptOperation.OP_SHA256:
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.SHA256;
                            break;
                        case ScriptOperation.OP_HASH160:
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.SHA256ThenRipeMd160;
                            break;
                        case ScriptOperation.OP_HASH256:
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.DoubleSHA256;
                            break;
                    }

                    IHashAlgorithm hashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(hashAlgorithmIdentifier);

                    byte[] dataToHash = mainStack.Pop();
                    ImmutableList<byte> hash = hashAlgorithm.CalculateHash(dataToHash);
                    mainStack.Push(hash.ToArray());
                    return true;
                }

                case ScriptOperation.OP_CODESEPARATOR:
                {
                    bytes.Sep();
                    return true;
                }

                case ScriptOperation.OP_CHECKSIG:
                case ScriptOperation.OP_CHECKSIGVERIFY:
                {
                    byte[] publicKey = mainStack.Pop();
                    byte[] signature = mainStack.Pop();

                    IImmutableList<byte> scriptCode = bytes.DataSinceLastSep;
                    scriptCode = scriptCode.DeleteAllOccurrencesOfSubsequence(signature);

                    mainStack.Push(signatureChecker.CheckSignature(signature, publicKey, scriptCode));
                    return opcode == ScriptOperation.OP_CHECKSIG ||
                           mainStack.Pop();
                }

                case ScriptOperation.OP_CHECKMULTISIG:
                case ScriptOperation.OP_CHECKMULTISIGVERIFY:
                {
                    BigInteger keyCount = mainStack.Pop();
                    if (mainStack.Count < keyCount)
                    {
                        return false;
                    }

                    LinkedList<byte[]> publicKeys = new LinkedList<byte[]>();
                    for (int i = 0; i < keyCount; i++)
                    {
                        publicKeys.AddLast(mainStack.Pop());
                    }

                    BigInteger signatureCount = mainStack.Pop();
                    if (mainStack.Count < signatureCount)
                    {
                        return false;
                    }

                    LinkedList<byte[]> signatures = new LinkedList<byte[]>();
                    for (int i = 0; i < signatureCount; i++)
                    {
                        signatures.AddLast(mainStack.Pop());
                    }

                    // The mainline client has a known bug in OP_CHECKMULTISIG.
                    // It pops an extra value off the stack before returning.
                    // So we need to do the same to maintain compatibility.
                    if (mainStack.Count < 1)
                    {
                        return false;
                    }

                    mainStack.Pop();

                    IImmutableList<byte> scriptCode = bytes.DataSinceLastSep;
                    scriptCode = signatures.Aggregate(scriptCode, ByteTwiddling.DeleteAllOccurrencesOfSubsequence);

                    int validSignatureCount = 0;

                    LinkedListNode<byte[]> signatureToValidate = signatures.First;
                    LinkedListNode<byte[]> publicKeyToAttempt = publicKeys.First;

                    while (publicKeyToAttempt != null &&
                           signatureToValidate != null)
                    {
                        byte[] signature = signatureToValidate.Value;
                        byte[] publicKey = publicKeyToAttempt.Value;

                        if (signatureChecker.CheckSignature(signature, publicKey, scriptCode))
                        {
                            validSignatureCount++;
                            signatureToValidate = signatureToValidate.Next;
                        }

                        publicKeyToAttempt = publicKeyToAttempt.Next;
                    }

                    mainStack.Push(validSignatureCount == signatureCount);
                    return opcode == ScriptOperation.OP_CHECKMULTISIG ||
                           mainStack.Pop();
                }

                #endregion Crypto

                default:
                    throw new InvalidOperationException("You should never see this at runtime!");
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

        private static bool TryReadBytes(IEnumerator<byte> bytes, byte[] value)
        {
            int currentIndex = 0;

            while (currentIndex < value.Length)
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
