using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

#if X64
using Secp256k1;
#else
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
#endif

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
            byte[] scriptBytes = ByteTwiddling.ConcatenateData(script.Select(ScriptOpToBytes));
            byte[] signatureBytes = signature.GetArray();
            byte hashType = signatureBytes.Last();
            Array.Resize(ref signatureBytes, signatureBytes.Length - 1);

            byte[] hashTypeBytes = { hashType, 0, 0, 0 };

            ProtocolTxIn[] inputs = new ProtocolTxIn[this.transaction.Inputs.Count];

            for (int i = 0; i < inputs.Length; i++)
            {
                IValueSpender spender = this.transaction.Inputs[i];

                if (spender.SpentTransactionIdentifier.NumericValue.IsZero)
                {
                    continue;
                }

                FancyByteArray originatingTransactionIdentifier = spender.SpentTransactionIdentifier;
                uint originatingTransactionOutputIndex = spender.SpentTransactionOutputIndex;
                IEnumerable<byte> scriptSig = i == this.outputIndex ? scriptBytes : Enumerable.Empty<byte>();
                uint seq = spender.SequenceNumber;
                inputs[i] = new ProtocolTxIn(originatingTransactionIdentifier, originatingTransactionOutputIndex, scriptSig, seq);
            }

            ProtocolTxOut[] outputs = new ProtocolTxOut[this.transaction.Outputs.Count];
            for (int i = 0; i < outputs.Length; i++)
            {
                IValueSource valueSource = this.transaction.Outputs[i];
                long availableValue = (long)valueSource.AvailableValue;
                byte[] scriptPubKey = valueSource.ScriptPublicKey;

                outputs[i] = new ProtocolTxOut(availableValue, scriptPubKey);
            }

            ProtocolTransaction tx = new ProtocolTransaction(this.transaction.Version, inputs, outputs, this.transaction.LockTime);

            var dataToHash = ByteTwiddling.ConcatenateData(tx.Data, hashTypeBytes);
            var hashedData = this.hashAlgorithm.CalculateHash(dataToHash);

#if X64
            Signatures.VerifyResult result = Signatures.Verify(hashedData, signatureBytes, publicKey.GetArray());
            return result == Signatures.VerifyResult.Verified;
#else
            var secp256k1 = SecNamedCurves.GetByName("secp256k1");
            var ecParams = new ECDomainParameters(secp256k1.Curve, secp256k1.G, secp256k1.N, secp256k1.H);
            ECPublicKeyParameters par = new ECPublicKeyParameters(secp256k1.Curve.DecodePoint(publicKey.GetArray()), ecParams);
            ECDsaSigner signer = new ECDsaSigner();
            signer.Init(false, par);
            DerInteger r, s;
            using (Asn1InputStream decoder = new Asn1InputStream(signatureBytes))
            {
                DerSequence seq = (DerSequence)decoder.ReadObject();
                r = (DerInteger)seq[0];
                s = (DerInteger)seq[1];
            }

            return signer.VerifySignature(hashedData, r.Value, s.Value);
#endif
        }

        private static byte[] ScriptOpToBytes(TransactionScriptOperation op)
        {
            byte[] opcodeBytes = { op.Opcode };
            byte[] sizeBytes = new byte[0];
            byte[] data = op.Data;

            switch (op.Opcode)
            {
                case (0x4c):
                {
                    sizeBytes = new[] { (byte)data.Length };
                    break;
                }

                case (0x4d):
                {
                    sizeBytes = BitConverter.GetBytes((ushort)data.Length).LittleEndianToOrFromBitConverterEndianness();
                    break;
                }

                case (0x4e):
                {
                    sizeBytes = BitConverter.GetBytes((uint)data.Length).LittleEndianToOrFromBitConverterEndianness();
                    break;
                }
            }

            return ByteTwiddling.ConcatenateData(opcodeBytes, sizeBytes, data);
        }
    }
}
