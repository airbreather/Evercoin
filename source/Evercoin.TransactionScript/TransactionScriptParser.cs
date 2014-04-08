using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptParser : ITransactionScriptParser
    {
        public ReadOnlyCollection<TransactionScriptOperation> Parse(IEnumerable<byte> bytes)
        {
            return ParseCore(bytes).ToList().AsReadOnly();
        }

        private static IEnumerable<TransactionScriptOperation> ParseCore(IEnumerable<byte> bytes)
        {
            byte[] scriptBytes = bytes.GetArray();

            for (int i = 0; i < scriptBytes.Length;)
            {
                byte opcodeByte = scriptBytes[i++];
                ScriptOpcode opcode = (ScriptOpcode)opcodeByte;

                if (opcode <= ScriptOpcode.END_OP_DATA &&
                    opcode >= ScriptOpcode.BEGIN_OP_DATA)
                {
                    byte dataSize = (byte)unchecked(opcodeByte - ScriptOpcode.BEGIN_OP_DATA);
                    if (i + dataSize > scriptBytes.Length)
                    {
                        yield return TransactionScriptOperation.Invalid;
                        yield break;
                    }

                    // Next {n} bytes contain the data to push.
                    IReadOnlyList<byte> data = scriptBytes.GetRange(i, dataSize);
                    yield return new TransactionScriptOperation(opcodeByte, data);
                    i += dataSize;
                    continue;
                }

                switch (opcode)
                {
                    case ScriptOpcode.OP_PUSHDATA1:
                    case ScriptOpcode.OP_PUSHDATA2:
                    case ScriptOpcode.OP_PUSHDATA4:
                    {
                        uint dataSize = 1;
                        switch (opcode)
                        {
                            case ScriptOpcode.OP_PUSHDATA1:
                            {
                                if (i + 1 > scriptBytes.Length)
                                {
                                    yield return TransactionScriptOperation.Invalid;
                                    yield break;
                                }

                                IReadOnlyList<byte> dataSizeBytes = scriptBytes.GetRange(i, 1);
                                dataSize = dataSizeBytes[0];
                                i += 1;
                                break;
                            }

                            case ScriptOpcode.OP_PUSHDATA2:
                            {
                                if (i + 2 > scriptBytes.Length)
                                {
                                    yield return TransactionScriptOperation.Invalid;
                                    yield break;
                                }

                                IReadOnlyList<byte> dataSizeBytes = scriptBytes.GetRange(i, 2);
                                dataSize = BitConverter.ToUInt16(dataSizeBytes.GetArray().LittleEndianToOrFromBitConverterEndianness(), 0);
                                i += 2;
                                break;
                            }

                            case ScriptOpcode.OP_PUSHDATA4:
                            {
                                if (i + 4 > scriptBytes.Length)
                                {
                                    yield return TransactionScriptOperation.Invalid;
                                    yield break;
                                }

                                IReadOnlyList<byte> dataSizeBytes = scriptBytes.GetRange(i, 4);
                                dataSize = BitConverter.ToUInt32(dataSizeBytes.GetArray().LittleEndianToOrFromBitConverterEndianness(), 0);
                                i += 4;
                                break;
                            }
                        }

                        int intDataSize = (int)dataSize;
                        if (i + intDataSize > scriptBytes.Length)
                        {
                            yield return TransactionScriptOperation.Invalid;
                            yield break;
                        }

                        IReadOnlyList<byte> data = scriptBytes.GetRange(i, intDataSize);
                        yield return new TransactionScriptOperation(opcodeByte, data);

                        i += intDataSize;
                        break;
                    }

                    default:
                        yield return new TransactionScriptOperation(opcodeByte);
                        break;
                }
            }
        }
    }
}
