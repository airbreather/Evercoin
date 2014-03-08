using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math.EC;

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

            byte[] hashTypeBytes = { hashType, 0, 0, 0};

            ImmutableList<ProtocolTxIn> inputs = ImmutableList.CreateRange(this.transaction.Inputs.Select((input, n) => new ProtocolTxIn(input.SpendingTransactionIdentifier, input.SpendingTransactionInputIndex, n == this.outputIndex ? script.Aggregate(ImmutableList<byte>.Empty, (prevData, nextOp) => prevData.AddRange(ScriptOpToBytes(nextOp))) : ImmutableList<byte>.Empty, input.SequenceNumber)));
            ImmutableList<ProtocolTxOut> outputs = ImmutableList.CreateRange(this.transaction.Outputs.Select(x => new ProtocolTxOut((long)x.AvailableValue, x.ScriptPublicKey)));
            ProtocolTransaction tx = new ProtocolTransaction(this.transaction.Version, inputs, outputs, this.transaction.LockTime);

            var secp256k1 = SecNamedCurves.GetByName("secp256k1");
            var ecParams = new ECDomainParameters(secp256k1.Curve, secp256k1.G, secp256k1.N, secp256k1.H);
            ECPublicKeyParameters par = new ECPublicKeyParameters(secp256k1.Curve.DecodePoint(publicKey.ToArray()), ecParams);
            ECDsaSigner signer = new ECDsaSigner();
            signer.Init(false, par);
            DerInteger r, s;
            using (Asn1InputStream decoder = new Asn1InputStream(signatureBytes.ToArray()))
            {
                DerSequence seq = (DerSequence)decoder.ReadObject();
                r = (DerInteger)seq[0];
                s = (DerInteger)seq[1];
            }

            var dataToHash = tx.Data.Concat(hashTypeBytes);
            var hashedData = this.hashAlgorithm.CalculateHash(dataToHash);
            var a1 = signer.VerifySignature(hashedData.ToArray(), r.Value, s.Value);
            return a1;
        }

        private static ImmutableList<byte> ScriptOpToBytes(TransactionScriptOperation op)
        {
            switch (op.Opcode)
            {
                case (0x4c):
                    {
                        return ImmutableList.Create(op.Opcode).AddRange(op.Data.Skip(1));
                    }

                case (0x4d):
                    {
                        return ImmutableList.Create(op.Opcode).AddRange(op.Data.Skip(2));
                    }

                case (0x4e):
                    {
                        return ImmutableList.Create(op.Opcode).AddRange(op.Data.Skip(4));
                    }

                default:
                {
                    return ImmutableList.Create(op.Opcode).AddRange(op.Data);
                }
            }
        }
    }
}
