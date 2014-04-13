using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Evercoin.BaseImplementations;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptRunner : TransactionScriptRunnerBase
    {
        #region Various Definitions

        /// <summary>
        /// The set of opcodes that we can't just completely skip on,
        /// if we're in an unexecuted conditional branch.
        /// </summary>
        private static readonly HashSet<ScriptOpcode> ConditionalOpcodes = new HashSet<ScriptOpcode>
                                                                           {
                                                                               ScriptOpcode.OP_IF,
                                                                               ScriptOpcode.OP_NOTIF,
                                                                               ScriptOpcode.OP_ELSE,
                                                                               ScriptOpcode.OP_ENDIF,
                                                                           };

        private static readonly HashSet<ScriptOpcode> DisabledOpcodes = new HashSet<ScriptOpcode>
                                                                        {
                                                                            ScriptOpcode.OP_CAT,
                                                                            ScriptOpcode.OP_SUBSTR,
                                                                            ScriptOpcode.OP_LEFT,
                                                                            ScriptOpcode.OP_RIGHT,
                                                                            ScriptOpcode.OP_INVERT,
                                                                            ScriptOpcode.OP_AND,
                                                                            ScriptOpcode.OP_OR,
                                                                            ScriptOpcode.OP_XOR,
                                                                            ScriptOpcode.OP_2MUL,
                                                                            ScriptOpcode.OP_2DIV,
                                                                            ScriptOpcode.OP_MUL,
                                                                            ScriptOpcode.OP_DIV,
                                                                            ScriptOpcode.OP_MOD,
                                                                            ScriptOpcode.OP_LSHIFT,
                                                                            ScriptOpcode.OP_RSHIFT,
                                                                            ScriptOpcode.OP_VERIF,
                                                                            ScriptOpcode.OP_VERNOTIF,
                                                                        };

        /// <summary>
        /// The number of items on the stack required to perform an operation.
        /// </summary>
        private static readonly Dictionary<ScriptOpcode, int> MinimumRequiredStackDepthForOperation = new Dictionary<ScriptOpcode, int>
                                                                                                      {
                                                                                                          { ScriptOpcode.OP_IF, 1 },
                                                                                                          { ScriptOpcode.OP_NOTIF, 1 },
                                                                                                          { ScriptOpcode.OP_VERIFY, 1 },
                                                                                                          { ScriptOpcode.OP_DROP, 1 },
                                                                                                          { ScriptOpcode.OP_DUP, 1 },
                                                                                                          { ScriptOpcode.OP_IFDUP, 1 },
                                                                                                          { ScriptOpcode.OP_2DROP, 2 },
                                                                                                          { ScriptOpcode.OP_2DUP, 2 },
                                                                                                          { ScriptOpcode.OP_3DUP, 3 },
                                                                                                          { ScriptOpcode.OP_2OVER, 4 },
                                                                                                          { ScriptOpcode.OP_2ROT, 6 },
                                                                                                          { ScriptOpcode.OP_2SWAP, 4 },
                                                                                                          { ScriptOpcode.OP_NIP, 2 },
                                                                                                          { ScriptOpcode.OP_OVER, 2 },
                                                                                                          { ScriptOpcode.OP_PICK, 1 },
                                                                                                          { ScriptOpcode.OP_ROLL, 1 },
                                                                                                          { ScriptOpcode.OP_ROT, 3 },
                                                                                                          { ScriptOpcode.OP_SWAP, 2 },
                                                                                                          { ScriptOpcode.OP_TUCK, 2 },
                                                                                                          { ScriptOpcode.OP_SIZE, 1 },
                                                                                                          { ScriptOpcode.OP_EQUAL, 2 },
                                                                                                          { ScriptOpcode.OP_EQUALVERIFY, 2 },
                                                                                                          { ScriptOpcode.OP_1ADD, 1 },
                                                                                                          { ScriptOpcode.OP_1SUB, 1 },
                                                                                                          { ScriptOpcode.OP_NEGATE, 1 },
                                                                                                          { ScriptOpcode.OP_ABS, 1 },
                                                                                                          { ScriptOpcode.OP_NOT, 1 },
                                                                                                          { ScriptOpcode.OP_0NOTEQUAL, 1 },
                                                                                                          { ScriptOpcode.OP_ADD, 2 },
                                                                                                          { ScriptOpcode.OP_SUB, 2 },
                                                                                                          { ScriptOpcode.OP_BOOLAND, 2 },
                                                                                                          { ScriptOpcode.OP_BOOLOR, 2 },
                                                                                                          { ScriptOpcode.OP_NUMEQUAL, 2 },
                                                                                                          { ScriptOpcode.OP_NUMEQUALVERIFY, 2 },
                                                                                                          { ScriptOpcode.OP_NUMNOTEQUAL, 2 },
                                                                                                          { ScriptOpcode.OP_LESSTHAN, 2 },
                                                                                                          { ScriptOpcode.OP_GREATERTHAN, 2 },
                                                                                                          { ScriptOpcode.OP_LESSTHANOREQUAL, 2 },
                                                                                                          { ScriptOpcode.OP_GREATERTHANOREQUAL, 2 },
                                                                                                          { ScriptOpcode.OP_MIN, 2 },
                                                                                                          { ScriptOpcode.OP_MAX, 2 },
                                                                                                          { ScriptOpcode.OP_WITHIN, 3 },
                                                                                                          { ScriptOpcode.OP_HASHALGORITHM1, 1 },
                                                                                                          { ScriptOpcode.OP_HASHALGORITHM2, 1 },
                                                                                                          { ScriptOpcode.OP_HASHALGORITHM3, 1 },
                                                                                                          { ScriptOpcode.OP_HASHALGORITHM4, 1 },
                                                                                                          { ScriptOpcode.OP_HASHALGORITHM5, 1 },
                                                                                                          { ScriptOpcode.OP_CHECKSIG, 2 },
                                                                                                          { ScriptOpcode.OP_CHECKSIGVERIFY, 2 },
                                                                                                          { ScriptOpcode.OP_CHECKMULTISIG, 1 },
                                                                                                          { ScriptOpcode.OP_CHECKMULTISIGVERIFY, 1 },
                                                                                                      };

        #endregion Various Definitions

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        private readonly IChainParameters chainParameters;

        public TransactionScriptRunner(IHashAlgorithmStore hashAlgorithmStore, IChainParameters chainParameters)
        {
            if (hashAlgorithmStore == null)
            {
                throw new ArgumentNullException("hashAlgorithmStore");
            }

            if (chainParameters == null)
            {
                throw new ArgumentNullException("chainParameters");
            }

            this.hashAlgorithmStore = hashAlgorithmStore;
            this.chainParameters = chainParameters;
        }

        public override ScriptEvaluationResult EvaluateScript(IEnumerable<TransactionScriptOperation> scriptOperations, ISignatureChecker signatureChecker, Stack<FancyByteArray> mainStack, Stack<FancyByteArray> alternateStack)
        {
            if (scriptOperations == null)
            {
                throw new ArgumentNullException("scriptOperations");
            }

            if (signatureChecker == null)
            {
                throw new ArgumentNullException("signatureChecker");
            }

            if (mainStack == null)
            {
                throw new ArgumentNullException("mainStack");
            }

            if (alternateStack == null)
            {
                throw new ArgumentNullException("alternateStack");
            }

            int afterLastSep = 0;
            Stack<bool> conditionalStack = new Stack<bool>();
            int i = 0;

            TransactionScriptOperation[] ops = scriptOperations.GetArray();
            return ops.All(scriptOperation => scriptOperation.IsValid &&
                                              this.Eval(ops, i++, ref afterLastSep, mainStack, alternateStack, conditionalStack, signatureChecker)) ?
                   new ScriptEvaluationResult(mainStack, alternateStack) :
                   ScriptEvaluationResult.False;
        }

        private bool Eval(TransactionScriptOperation[] ops, int pos, ref int lastSep, Stack<FancyByteArray> mainStack, Stack<FancyByteArray> alternateStack, Stack<bool> conditionalStack, ISignatureChecker signatureChecker)
        {
            TransactionScriptOperation op = ops[pos];
            ScriptOpcode opcode = (ScriptOpcode)op.Opcode;
            bool actuallyExecute = conditionalStack.All(x => x);

            if (!actuallyExecute &&
                !ConditionalOpcodes.Contains(opcode))
            {
                bool opcodeIsDisabled = DisabledOpcodes.Contains(opcode) ||
                                        (opcode >= ScriptOpcode.BEGIN_UNUSED &&
                                         opcode <= ScriptOpcode.END_UNUSED);
                return !opcodeIsDisabled;
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

            if (opcode <= ScriptOpcode.END_OP_DATA &&
                opcode >= ScriptOpcode.BEGIN_OP_DATA)
            {
                // Next {n} bytes contain the data to push.
                mainStack.Push(op.Data);
                return true;
            }

            if (opcode >= ScriptOpcode.BEGIN_UNUSED &&
                opcode <= ScriptOpcode.END_UNUSED)
            {
                return false;
            }

            // Braces are added to case statements only where the case handler
            // defines a new local variable.
            switch (opcode)
            {
                #region NOOP

                case ScriptOpcode.OP_NOP:
                case ScriptOpcode.OP_NOP1:
                case ScriptOpcode.OP_NOP2:
                case ScriptOpcode.OP_NOP3:
                case ScriptOpcode.OP_NOP4:
                case ScriptOpcode.OP_NOP5:
                case ScriptOpcode.OP_NOP6:
                case ScriptOpcode.OP_NOP7:
                case ScriptOpcode.OP_NOP8:
                case ScriptOpcode.OP_NOP9:
                case ScriptOpcode.OP_NOP10:
                    return true;

                #endregion NOOP

                #region Disabled

                // These are all disabled unconditionally.
                case ScriptOpcode.OP_CAT:
                case ScriptOpcode.OP_SUBSTR:
                case ScriptOpcode.OP_LEFT:
                case ScriptOpcode.OP_RIGHT:
                case ScriptOpcode.OP_INVERT:
                case ScriptOpcode.OP_AND:
                case ScriptOpcode.OP_OR:
                case ScriptOpcode.OP_XOR:
                case ScriptOpcode.OP_2MUL:
                case ScriptOpcode.OP_2DIV:
                case ScriptOpcode.OP_MUL:
                case ScriptOpcode.OP_DIV:
                case ScriptOpcode.OP_MOD:
                case ScriptOpcode.OP_LSHIFT:
                case ScriptOpcode.OP_RSHIFT:
                case ScriptOpcode.OP_VERIF:
                case ScriptOpcode.OP_VERNOTIF:

                // These are all disabled if they're in an executed branch:
                case ScriptOpcode.OP_VER:
                case ScriptOpcode.OP_RETURN:
                case ScriptOpcode.OP_RESERVED:
                case ScriptOpcode.OP_RESERVED1:
                case ScriptOpcode.OP_RESERVED2:
                    return false;

                #endregion Disabled

                #region Push Value

                case ScriptOpcode.OP_1NEGATE:
                case ScriptOpcode.OP_1:
                case ScriptOpcode.OP_2:
                case ScriptOpcode.OP_3:
                case ScriptOpcode.OP_4:
                case ScriptOpcode.OP_5:
                case ScriptOpcode.OP_6:
                case ScriptOpcode.OP_7:
                case ScriptOpcode.OP_8:
                case ScriptOpcode.OP_9:
                case ScriptOpcode.OP_10:
                case ScriptOpcode.OP_11:
                case ScriptOpcode.OP_12:
                case ScriptOpcode.OP_13:
                case ScriptOpcode.OP_14:
                case ScriptOpcode.OP_15:
                case ScriptOpcode.OP_16:
                {
                    byte valueToPush = opcode - ScriptOpcode.OPCODE_IMMEDIATELY_BEFORE_OP_1;
                    mainStack.Push(FancyByteArray.CreateFromBytes(valueToPush.AsSingleElementEnumerable()));
                    return true;
                }

                #endregion Push Value

                #region Control Flow

                case ScriptOpcode.OP_IF:
                case ScriptOpcode.OP_NOTIF:
                {
                    if (!actuallyExecute)
                    {
                        conditionalStack.Push(false);
                        return true;
                    }

                    bool conditional = mainStack.Pop() ^
                                       (opcode == ScriptOpcode.OP_NOTIF);

                    conditionalStack.Push(conditional);
                    return true;
                }

                case ScriptOpcode.OP_ELSE:
                case ScriptOpcode.OP_ENDIF:
                {
                    if (conditionalStack.Count < 1)
                    {
                        return false;
                    }

                    bool lastCondition = conditionalStack.Pop();

                    if (opcode == ScriptOpcode.OP_ELSE)
                    {
                        conditionalStack.Push(!lastCondition);
                    }

                    return true;
                }

                #endregion Control Flow

                #region Stack Twiddling

                case ScriptOpcode.OP_TOALTSTACK:
                    return MoveItemFromStackToStack(mainStack, alternateStack);

                case ScriptOpcode.OP_FROMALTSTACK:
                    return MoveItemFromStackToStack(alternateStack, mainStack);

                case ScriptOpcode.OP_DROP:
                    mainStack.Pop();
                    return true;

                case ScriptOpcode.OP_DUP:
                case ScriptOpcode.OP_IFDUP:
                {
                    FancyByteArray item = mainStack.Peek();
                    if (opcode == ScriptOpcode.OP_DUP || item)
                    {
                        mainStack.Push(item);
                    }

                    return true;
                }

                case ScriptOpcode.OP_DEPTH:
                {
                    int depth = mainStack.Count;
                    mainStack.Push(BitConverter.GetBytes(depth).LittleEndianToOrFromBitConverterEndianness());
                    return true;
                }

                case ScriptOpcode.OP_SIZE:
                {
                    FancyByteArray item = mainStack.Peek();
                    int size = item.Value.Length;

                    mainStack.Push(BitConverter.GetBytes(size).LittleEndianToOrFromBitConverterEndianness());
                    return true;
                }

                case ScriptOpcode.OP_2DROP:
                case ScriptOpcode.OP_2DUP:
                {
                    FancyByteArray firstItem = mainStack.Pop();
                    FancyByteArray secondItem = mainStack.Pop();

                    if (opcode == ScriptOpcode.OP_2DUP)
                    {
                        mainStack.Push(secondItem);
                        mainStack.Push(firstItem);
                        mainStack.Push(secondItem);
                        mainStack.Push(firstItem);
                    }

                    return true;
                }

                case ScriptOpcode.OP_3DUP:
                {
                    FancyByteArray firstItem = mainStack.Pop();
                    FancyByteArray secondItem = mainStack.Pop();
                    FancyByteArray thirdItem = mainStack.Pop();

                    mainStack.Push(thirdItem);
                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    mainStack.Push(thirdItem);
                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);

                    return true;
                }

                case ScriptOpcode.OP_2OVER:
                {
                    FancyByteArray firstItem = mainStack.Pop();
                    FancyByteArray secondItem = mainStack.Pop();
                    FancyByteArray thirdItem = mainStack.Pop();
                    FancyByteArray fourthItem = mainStack.Pop();

                    mainStack.Push(fourthItem);
                    mainStack.Push(thirdItem);
                    mainStack.Push(fourthItem);
                    mainStack.Push(thirdItem);
                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);

                    return true;
                }

                case ScriptOpcode.OP_2ROT:
                {
                    FancyByteArray firstItem = mainStack.Pop();
                    FancyByteArray secondItem = mainStack.Pop();
                    FancyByteArray thirdItem = mainStack.Pop();
                    FancyByteArray fourthItem = mainStack.Pop();
                    FancyByteArray fifthItem = mainStack.Pop();
                    FancyByteArray sixthItem = mainStack.Pop();

                    mainStack.Push(fourthItem);
                    mainStack.Push(thirdItem);
                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    mainStack.Push(sixthItem);
                    mainStack.Push(fifthItem);

                    return true;
                }

                case ScriptOpcode.OP_2SWAP:
                {
                    FancyByteArray firstItem = mainStack.Pop();
                    FancyByteArray secondItem = mainStack.Pop();
                    FancyByteArray thirdItem = mainStack.Pop();
                    FancyByteArray fourthItem = mainStack.Pop();

                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    mainStack.Push(fourthItem);
                    mainStack.Push(thirdItem);

                    return true;
                }

                case ScriptOpcode.OP_NIP:
                case ScriptOpcode.OP_OVER:
                {
                    FancyByteArray innocentBystander = mainStack.Pop();
                    FancyByteArray secondFromTop = mainStack.Pop();
                    mainStack.Push(innocentBystander);

                    if (opcode == ScriptOpcode.OP_OVER)
                    {
                        mainStack.Push(secondFromTop);
                    }

                    return true;
                }

                case ScriptOpcode.OP_PICK:
                case ScriptOpcode.OP_ROLL:
                {
                    BigInteger fetchDepth = mainStack.Pop();
                    if (mainStack.Count <= fetchDepth)
                    {
                        return false;
                    }

                    // we need somewhere to store all the stack items we pop,
                    // so we can push them back afterwards.
                    Stack<FancyByteArray> temporaryStack = new Stack<FancyByteArray>();
                    while (fetchDepth-- > 0)
                    {
                        FancyByteArray nextItem = mainStack.Pop();
                        temporaryStack.Push(nextItem);
                    }

                    FancyByteArray chosenItem = mainStack.Peek();
                    if (opcode == ScriptOpcode.OP_ROLL)
                    {
                        mainStack.Pop();
                    }

                    while (temporaryStack.Count > 0)
                    {
                        FancyByteArray nextItem = temporaryStack.Pop();
                        mainStack.Push(nextItem);
                    }

                    mainStack.Push(chosenItem);
                    return true;
                }

                case ScriptOpcode.OP_ROT:
                {
                    FancyByteArray firstItem = mainStack.Pop();
                    FancyByteArray secondItem = mainStack.Pop();
                    FancyByteArray thirdItem = mainStack.Pop();

                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    mainStack.Push(thirdItem);

                    return true;
                }

                case ScriptOpcode.OP_SWAP:
                case ScriptOpcode.OP_TUCK:
                {
                    FancyByteArray firstItem = mainStack.Pop();
                    FancyByteArray secondItem = mainStack.Pop();

                    if (opcode == ScriptOpcode.OP_TUCK)
                    {
                        mainStack.Push(firstItem);
                    }

                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    return true;
                }

                #endregion Stack Twiddling

                #region Boolean

                case ScriptOpcode.OP_EQUAL:
                case ScriptOpcode.OP_EQUALVERIFY:
                {
                    FancyByteArray firstItem = mainStack.Pop();
                    FancyByteArray secondItem = mainStack.Pop();

                    mainStack.Push(firstItem == secondItem);

                    return opcode == ScriptOpcode.OP_EQUAL ||
                           mainStack.Pop();
                }

                case ScriptOpcode.OP_VERIFY:
                    return mainStack.Pop();

                #endregion Boolean

                #region Arithmetic

                case ScriptOpcode.OP_1ADD:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(item + 1, Endianness.LittleEndian));
                    return true;
                }

                case ScriptOpcode.OP_1SUB:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(item - 1, Endianness.LittleEndian));
                    return true;
                }

                case ScriptOpcode.OP_NEGATE:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(-item, Endianness.LittleEndian));
                    return true;
                }

                case ScriptOpcode.OP_ABS:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(item.Sign < 0 ? -item : item, Endianness.LittleEndian));
                    return true;
                }

                case ScriptOpcode.OP_NOT:
                {
                    bool value = mainStack.Pop();
                    mainStack.Push(!value);
                    return true;
                }

                case ScriptOpcode.OP_0NOTEQUAL:
                {
                    bool value = mainStack.Pop();
                    mainStack.Push(value);
                    return true;
                }

                case ScriptOpcode.OP_ADD:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(firstValue + secondValue, Endianness.LittleEndian));
                    return true;
                }

                case ScriptOpcode.OP_SUB:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(firstValue - secondValue, Endianness.LittleEndian));
                    return true;
                }

                case ScriptOpcode.OP_BOOLAND:
                {
                    bool firstValue = mainStack.Pop();
                    bool secondValue = mainStack.Pop();
                    mainStack.Push(firstValue && secondValue);
                    return true;
                }

                case ScriptOpcode.OP_BOOLOR:
                {
                    bool firstValue = mainStack.Pop();
                    bool secondValue = mainStack.Pop();
                    mainStack.Push(firstValue || secondValue);
                    return true;
                }

                case ScriptOpcode.OP_NUMEQUAL:
                case ScriptOpcode.OP_NUMEQUALVERIFY:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue == secondValue);
                    return true;
                }

                case ScriptOpcode.OP_NUMNOTEQUAL:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue != secondValue);
                    return true;
                }

                case ScriptOpcode.OP_LESSTHAN:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue < secondValue);
                    return true;
                }

                case ScriptOpcode.OP_GREATERTHAN:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue > secondValue);
                    return true;
                }

                case ScriptOpcode.OP_LESSTHANOREQUAL:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue <= secondValue);
                    return true;
                }

                case ScriptOpcode.OP_GREATERTHANOREQUAL:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue >= secondValue);
                    return true;
                }

                case ScriptOpcode.OP_MIN:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(BigInteger.Min(firstValue, secondValue), Endianness.LittleEndian));
                    return true;
                }

                case ScriptOpcode.OP_MAX:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(BigInteger.Max(firstValue, secondValue), Endianness.LittleEndian));
                    return true;
                }

                case ScriptOpcode.OP_WITHIN:
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

                case ScriptOpcode.OP_HASHALGORITHM1:
                case ScriptOpcode.OP_HASHALGORITHM2:
                case ScriptOpcode.OP_HASHALGORITHM3:
                case ScriptOpcode.OP_HASHALGORITHM4:
                case ScriptOpcode.OP_HASHALGORITHM5:
                {

                    Guid hashAlgorithmIdentifier = Guid.Empty;
                    switch (opcode)
                    {
                        case ScriptOpcode.OP_HASHALGORITHM1:
                            hashAlgorithmIdentifier = this.chainParameters.ScriptHashAlgorithmIdentifier1;
                            break;
                        case ScriptOpcode.OP_HASHALGORITHM2:
                            hashAlgorithmIdentifier = this.chainParameters.ScriptHashAlgorithmIdentifier2;
                            break;
                        case ScriptOpcode.OP_HASHALGORITHM3:
                            hashAlgorithmIdentifier = this.chainParameters.ScriptHashAlgorithmIdentifier3;
                            break;
                        case ScriptOpcode.OP_HASHALGORITHM4:
                            hashAlgorithmIdentifier = this.chainParameters.ScriptHashAlgorithmIdentifier4;
                            break;
                        case ScriptOpcode.OP_HASHALGORITHM5:
                            hashAlgorithmIdentifier = this.chainParameters.ScriptHashAlgorithmIdentifier5;
                            break;
                    }

                    IHashAlgorithm hashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(hashAlgorithmIdentifier);

                    byte[] dataToHash = mainStack.Pop();
                    FancyByteArray hash = hashAlgorithm.CalculateHash(dataToHash);
                    mainStack.Push(hash);
                    return true;
                }

                case ScriptOpcode.OP_CODESEPARATOR:
                {
                    lastSep = pos;
                    return true;
                }

                case ScriptOpcode.OP_CHECKSIG:
                case ScriptOpcode.OP_CHECKSIGVERIFY:
                {
                    FancyByteArray publicKey = mainStack.Pop();
                    FancyByteArray signature = mainStack.Pop();

                    IEnumerable<TransactionScriptOperation> subscript = ops.GetRange(lastSep, ops.Length - lastSep)
                                                                           .ExceptWhere(o => o.Opcode == (byte)ScriptOpcode.OP_CODESEPARATOR || signature.Equals(o.Data));

                    mainStack.Push(signatureChecker.CheckSignature(signature.Value, publicKey.Value, subscript));
                    return opcode == ScriptOpcode.OP_CHECKSIG ||
                           mainStack.Pop();
                }

                case ScriptOpcode.OP_CHECKMULTISIG:
                case ScriptOpcode.OP_CHECKMULTISIGVERIFY:
                {
                    BigInteger keyCount = mainStack.Pop();
                    if (mainStack.Count < keyCount)
                    {
                        return false;
                    }

                    LinkedList<FancyByteArray> publicKeys = new LinkedList<FancyByteArray>();
                    for (int i = 0; i < keyCount; i++)
                    {
                        publicKeys.AddLast(mainStack.Pop());
                    }

                    BigInteger signatureCount = mainStack.Pop();
                    if (mainStack.Count < signatureCount)
                    {
                        return false;
                    }

                    LinkedList<FancyByteArray> signatures = new LinkedList<FancyByteArray>();
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

                    HashSet<FancyByteArray> signatureSet = new HashSet<FancyByteArray>(signatures);
                    List<TransactionScriptOperation> subscript = ops.GetRange(lastSep, ops.Length - lastSep)
                                                                    .ExceptWhere(o => o.Opcode == (byte)ScriptOpcode.OP_CODESEPARATOR || signatureSet.Contains(o.Data))
                                                                    .ToList();

                    int validSignatureCount = 0;

                    LinkedListNode<FancyByteArray> signatureToValidate = signatures.First;
                    LinkedListNode<FancyByteArray> publicKeyToAttempt = publicKeys.First;

                    while (publicKeyToAttempt != null &&
                           signatureToValidate != null)
                    {
                        byte[] signature = signatureToValidate.Value;
                        byte[] publicKey = publicKeyToAttempt.Value;

                        if (signatureChecker.CheckSignature(signature, publicKey, subscript))
                        {
                            validSignatureCount++;
                            signatureToValidate = signatureToValidate.Next;
                        }

                        publicKeyToAttempt = publicKeyToAttempt.Next;
                    }

                    mainStack.Push(validSignatureCount == signatureCount);
                    return opcode == ScriptOpcode.OP_CHECKMULTISIG ||
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
    }
}
