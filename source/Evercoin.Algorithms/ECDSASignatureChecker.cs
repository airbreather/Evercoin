using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

using Secp256k1;

namespace Evercoin.Algorithms
{
    internal sealed class ECDSASignatureChecker : ISignatureChecker
    {
        private readonly IHashAlgorithm hashAlgorithm;

        private readonly ITransaction transaction;

        private readonly int outputIndex;

        public ECDSASignatureChecker(IHashAlgorithm hashAlgorithm, ITransaction transaction, int outputIndex)
        {
            this.hashAlgorithm = hashAlgorithm;
            this.transaction = transaction;
            this.outputIndex = outputIndex;
        }

        public bool CheckSignature(IEnumerable<byte> signature, IEnumerable<byte> publicKey, IEnumerable<TransactionScriptOperation> script)
        {
            ImmutableList<byte> signatureBytes = signature.ToImmutableList();
            byte hashType = signatureBytes[signatureBytes.Count - 1];
            signatureBytes = signatureBytes.GetRange(0, signatureBytes.Count - 1);

            byte[] hashTypeBytes = { hashType, 0, 0, 0 };

            ImmutableList<ProtocolTxIn> inputs = ImmutableList.CreateRange(this.transaction.Inputs.Select((input, n) => new ProtocolTxIn(input.SpendingTransactionIdentifier, input.SpendingTransactionInputIndex, n == this.outputIndex ? script.Aggregate(ImmutableList<byte>.Empty, (prevData, nextOp) => prevData.AddRange(ScriptOpToBytes(nextOp))) : ImmutableList<byte>.Empty, input.SequenceNumber)));
            ImmutableList<ProtocolTxOut> outputs = ImmutableList.CreateRange(this.transaction.Outputs.Select(x => new ProtocolTxOut((long)x.AvailableValue, x.ScriptPublicKey)));
            ProtocolTransaction tx = new ProtocolTransaction(this.transaction.Version, inputs, outputs, this.transaction.LockTime);

            var dataToHash = tx.Data.Concat(hashTypeBytes);
            var hashedData = this.hashAlgorithm.CalculateHash(dataToHash);
            return Signatures.Verify(hashedData.ToArray(), signatureBytes.ToArray(), publicKey.ToArray()) == Signatures.VerifyResult.Verified;
        }

        private static ImmutableList<byte> ScriptOpToBytes(TransactionScriptOperation op)
        {
            switch (op.Opcode)
            {
                case (0x4c):
                {
                    return ImmutableList.Create(op.Opcode).Add((byte)op.Data.Count).AddRange(op.Data);
                }

                case (0x4d):
                {
                    byte[] size = BitConverter.GetBytes((ushort)op.Data.Count).LittleEndianToOrFromBitConverterEndianness();
                    return ImmutableList.Create(op.Opcode).AddRange(size).AddRange(op.Data);
                }

                case (0x4e):
                {
                    byte[] size = BitConverter.GetBytes((uint)op.Data.Count).LittleEndianToOrFromBitConverterEndianness();
                    return ImmutableList.Create(op.Opcode).AddRange(size).AddRange(op.Data);
                }

                default:
                {
                    return ImmutableList.Create(op.Opcode).AddRange(op.Data);
                }
            }
        }
    }
}
