using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private readonly ITransactionScriptParser transactionScriptParser;

        public TransactionScriptRunner(IHashAlgorithmStore hashAlgorithmStore, ITransactionScriptParser transactionScriptParser)
        {
            if (hashAlgorithmStore == null)
            {
                throw new ArgumentNullException("hashAlgorithmStore");
            }

            if (transactionScriptParser == null)
            {
                throw new ArgumentNullException("transactionScriptParser");
            }

            this.hashAlgorithmStore = hashAlgorithmStore;
            this.transactionScriptParser = transactionScriptParser;
        }

        public override ScriptEvaluationResult EvaluateScript(IEnumerable<byte> serializedScript, ISignatureChecker signatureChecker, Stack<StackItem> mainStack, Stack<StackItem> alternateStack)
        {
            if (serializedScript == null)
            {
                throw new ArgumentNullException("serializedScript");
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

            ImmutableList<TransactionScriptOperation> scriptOperations = this.transactionScriptParser.Parse(serializedScript);

            int afterLastSep = 0;
            Stack<bool> conditionalStack = new Stack<bool>();
            int i = 0;
            return scriptOperations.All(scriptOperation => scriptOperation.IsValid &&
                                                           this.Eval(scriptOperations, i++, ref afterLastSep, mainStack, alternateStack, conditionalStack, signatureChecker)) ?
                new ScriptEvaluationResult(mainStack, alternateStack) :
                ScriptEvaluationResult.False;
        }

        private bool Eval(ImmutableList<TransactionScriptOperation> ops, int pos, ref int lastSep, Stack<StackItem> mainStack, Stack<StackItem> alternateStack, Stack<bool> conditionalStack, ISignatureChecker signatureChecker)
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
                mainStack.Push(new StackItem(op.Data));
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
                case ScriptOpcode.OP_PUSHDATA1:
                case ScriptOpcode.OP_PUSHDATA2:
                case ScriptOpcode.OP_PUSHDATA4:
                    mainStack.Push(new StackItem(op.Data));
                    return true;

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
                    BigInteger valueToPush = opcode - ScriptOpcode.OPCODE_IMMEDIATELY_BEFORE_OP_1;
                    mainStack.Push(valueToPush);
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
                    StackItem item = mainStack.Peek();
                    if (opcode == ScriptOpcode.OP_DUP || item)
                    {
                        mainStack.Push(item);
                    }

                    return true;
                }

                case ScriptOpcode.OP_DEPTH:
                {
                    BigInteger depth = mainStack.Count;
                    mainStack.Push(depth);
                    return true;
                }

                case ScriptOpcode.OP_SIZE:
                {
                    ImmutableList<byte> item = mainStack.Peek();
                    BigInteger size = item.Count;

                    mainStack.Push(size);
                    return true;
                }

                case ScriptOpcode.OP_2DROP:
                case ScriptOpcode.OP_2DUP:
                {
                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();

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

                case ScriptOpcode.OP_2OVER:
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

                case ScriptOpcode.OP_2ROT:
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

                case ScriptOpcode.OP_2SWAP:
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

                case ScriptOpcode.OP_NIP:
                case ScriptOpcode.OP_OVER:
                {
                    StackItem innocentBystander = mainStack.Pop();
                    StackItem secondFromTop = mainStack.Pop();
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
                    Stack<StackItem> temporaryStack = new Stack<StackItem>();
                    while (fetchDepth-- > 0)
                    {
                        StackItem nextItem = mainStack.Pop();
                        temporaryStack.Push(nextItem);
                    }

                    StackItem chosenItem = mainStack.Peek();
                    if (opcode == ScriptOpcode.OP_ROLL)
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

                case ScriptOpcode.OP_ROT:
                {
                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();
                    StackItem thirdItem = mainStack.Pop();

                    mainStack.Push(secondItem);
                    mainStack.Push(firstItem);
                    mainStack.Push(thirdItem);

                    return true;
                }

                case ScriptOpcode.OP_SWAP:
                case ScriptOpcode.OP_TUCK:
                {
                    StackItem firstItem = mainStack.Pop();
                    StackItem secondItem = mainStack.Pop();

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
                    ImmutableList<byte> firstItem = mainStack.Pop();
                    ImmutableList<byte> secondItem = mainStack.Pop();

                    mainStack.Push(firstItem.SequenceEqual(secondItem));

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
                    mainStack.Push(item + 1);
                    return true;
                }

                case ScriptOpcode.OP_1SUB:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(item - 1);
                    return true;
                }

                case ScriptOpcode.OP_NEGATE:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(-item);
                    return true;
                }

                case ScriptOpcode.OP_ABS:
                {
                    BigInteger item = mainStack.Pop();
                    mainStack.Push(item.Sign < 0 ? -item : item);
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
                    mainStack.Push(firstValue + secondValue);
                    return true;
                }

                case ScriptOpcode.OP_SUB:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(firstValue - secondValue);
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
                    mainStack.Push(BigInteger.Min(firstValue, secondValue));
                    return true;
                }

                case ScriptOpcode.OP_MAX:
                {
                    BigInteger firstValue = mainStack.Pop();
                    BigInteger secondValue = mainStack.Pop();
                    mainStack.Push(BigInteger.Max(firstValue, secondValue));
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
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.RipeMd160;
                            break;
                        case ScriptOpcode.OP_HASHALGORITHM2:
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.SHA1;
                            break;
                        case ScriptOpcode.OP_HASHALGORITHM3:
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.SHA256;
                            break;
                        case ScriptOpcode.OP_HASHALGORITHM4:
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.SHA256ThenRipeMd160;
                            break;
                        case ScriptOpcode.OP_HASHALGORITHM5:
                            hashAlgorithmIdentifier = HashAlgorithmIdentifiers.DoubleSHA256;
                            break;
                    }

                    IHashAlgorithm hashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(hashAlgorithmIdentifier);

                    ImmutableList<byte> dataToHash = mainStack.Pop();
                    ImmutableList<byte> hash = hashAlgorithm.CalculateHash(dataToHash);
                    mainStack.Push(hash.ToArray());
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
                    ImmutableList<byte> publicKey = mainStack.Pop();
                    ImmutableList<byte> signature = mainStack.Pop();

                    TransactionScriptOperation sigOp = GetDataPushOp(signature);

                    ImmutableList<TransactionScriptOperation> subscript = ops.GetRange(lastSep, ops.Count - lastSep)
                                                                             .RemoveAll(o => o.Opcode == (byte)ScriptOpcode.OP_CODESEPARATOR ||
                                                                                             sigOp.Equals(o));

                    mainStack.Push(signatureChecker.CheckSignature(signature, publicKey, subscript));
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

                    LinkedList<ImmutableList<byte>> publicKeys = new LinkedList<ImmutableList<byte>>();
                    for (int i = 0; i < keyCount; i++)
                    {
                        publicKeys.AddLast(mainStack.Pop());
                    }

                    BigInteger signatureCount = mainStack.Pop();
                    if (mainStack.Count < signatureCount)
                    {
                        return false;
                    }

                    LinkedList<ImmutableList<byte>> signatures = new LinkedList<ImmutableList<byte>>();
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

                    ImmutableList<TransactionScriptOperation> subscript = ops.GetRange(lastSep, ops.Count - lastSep)
                                                                             .RemoveAll(o => o.Opcode == (byte)ScriptOpcode.OP_CODESEPARATOR);
                    subscript = signatures.Aggregate(subscript, (prevSubscript, nextSig) => prevSubscript.RemoveAll(sig => GetDataPushOp(nextSig).Equals(sig)));

                    int validSignatureCount = 0;

                    LinkedListNode<ImmutableList<byte>> signatureToValidate = signatures.First;
                    LinkedListNode<ImmutableList<byte>> publicKeyToAttempt = publicKeys.First;

                    while (publicKeyToAttempt != null &&
                           signatureToValidate != null)
                    {
                        ImmutableList<byte> signature = signatureToValidate.Value;
                        ImmutableList<byte> publicKey = publicKeyToAttempt.Value;

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

        private static TransactionScriptOperation GetDataPushOp(ImmutableList<byte> dataToPush)
        {
            if (dataToPush.Count <= (byte)ScriptOpcode.END_OP_DATA)
            {
                return new TransactionScriptOperation((byte)dataToPush.Count, dataToPush);
            }

            if (dataToPush.Count <= 0xff)
            {
                return new TransactionScriptOperation((byte)ScriptOpcode.OP_PUSHDATA1, dataToPush);
            }

            if (dataToPush.Count <= 0xffff)
            {
                return new TransactionScriptOperation((byte)ScriptOpcode.OP_PUSHDATA2, dataToPush);
            }

            return new TransactionScriptOperation((byte)ScriptOpcode.OP_PUSHDATA4, dataToPush);
        }
    }
}
