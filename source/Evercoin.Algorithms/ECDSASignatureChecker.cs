using System;
using System.Collections.Generic;
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
            byte[] signatureBytes = signature.GetArray();
            byte hashType = signatureBytes.Last();
            Array.Resize(ref signatureBytes, signatureBytes.Length - 1);

            byte[] hashTypeBytes = { hashType, 0, 0, 0 };

            ProtocolTxIn[] inputs = this.transaction.Inputs.Select((input, n) => new ProtocolTxIn(input.SpendingTransactionIdentifier, input.SpendingTransactionInputIndex, n == this.outputIndex ? ByteTwiddling.ConcatenateData(script.Select(ScriptOpToBytes)) : Enumerable.Empty<byte>(), input.SequenceNumber)).GetArray();
            ProtocolTxOut[] outputs = this.transaction.Outputs.Select(x => new ProtocolTxOut((long)x.AvailableValue, x.ScriptPublicKey)).GetArray();
            ProtocolTransaction tx = new ProtocolTransaction(this.transaction.Version, inputs, outputs, this.transaction.LockTime);

            var dataToHash = ByteTwiddling.ConcatenateData(tx.Data, hashTypeBytes);
            var hashedData = this.hashAlgorithm.CalculateHash(dataToHash);
            return Signatures.Verify(hashedData, signatureBytes, publicKey.GetArray()) == Signatures.VerifyResult.Verified;
        }

        private static byte[] ScriptOpToBytes(TransactionScriptOperation op)
        {
            byte[] opcodeBytes = { op.Opcode };
            byte[] sizeBytes = new byte[0];
            switch (op.Opcode)
            {
                case (0x4c):
                {
                    sizeBytes = new[] { (byte)op.Data.Length };
                    break;
                }

                case (0x4d):
                {
                    sizeBytes = BitConverter.GetBytes((ushort)op.Data.Length).LittleEndianToOrFromBitConverterEndianness();
                    break;
                }

                case (0x4e):
                {
                    sizeBytes = BitConverter.GetBytes((uint)op.Data.Length).LittleEndianToOrFromBitConverterEndianness();
                    break;
                }
            }

            return ByteTwiddling.ConcatenateData(opcodeBytes, sizeBytes, op.Data);
        }
    }
}
