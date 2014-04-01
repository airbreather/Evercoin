using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Evercoin
{
    public interface IBlockChain
    {
        ulong Length { get; }

        BigInteger? GetIdentifierOfBlockAtHeight(ulong height);

        BigInteger? GetIdentifierOfBlockWithTransaction(BigInteger transactionIdentifier);

        ulong? GetHeightOfBlock(BigInteger blockIdentifier);

        void AddBlockAtHeight(BigInteger block, ulong height);

        void AddTransactionToBlock(BigInteger transactionIdentifier, BigInteger blockIdentifier, ulong index);
    }

    public sealed class BlockChain : IBlockChain
    {
        private readonly ConcurrentDictionary<BigInteger, List<BigInteger>> blockToTransactionMapping = new ConcurrentDictionary<BigInteger, List<BigInteger>>();

        private readonly ConcurrentDictionary<BigInteger, BigInteger> transactionToBlockMapping = new ConcurrentDictionary<BigInteger, BigInteger>();

        private readonly ConcurrentDictionary<BigInteger, ulong> blockToHeightMapping = new ConcurrentDictionary<BigInteger, ulong>();

        private readonly ConcurrentDictionary<ulong, BigInteger> heightToBlockMapping = new ConcurrentDictionary<ulong, BigInteger>();

        private long length = Int64.MinValue + 1;

        public ulong Length { get { return (ulong)this.length + Int64.MaxValue; } }

        public BigInteger? GetIdentifierOfBlockAtHeight(ulong height)
        {
            BigInteger result;
            return this.heightToBlockMapping.TryGetValue(height, out result) ?
                   result :
                   default(BigInteger?);
        }

        public BigInteger? GetIdentifierOfBlockWithTransaction(BigInteger transactionIdentifier)
        {
            BigInteger result;
            return this.transactionToBlockMapping.TryGetValue(transactionIdentifier, out result) ?
                result :
                default(BigInteger?);
        }

        public ulong? GetHeightOfBlock(BigInteger blockIdentifier)
        {
            ulong result;
            return this.blockToHeightMapping.TryGetValue(blockIdentifier, out result) ?
                   result :
                   default(ulong?);
        }

        public void AddBlockAtHeight(BigInteger blockIdentifier, ulong height)
        {
            this.blockToHeightMapping[blockIdentifier] = height;
            this.heightToBlockMapping[height] = blockIdentifier;
            ////this.blockToTransactionMapping[blockIdentifier] = new List<BigInteger>();
            Interlocked.Increment(ref length);
        }

        public void AddTransactionToBlock(BigInteger transactionIdentifier, BigInteger blockIdentifier, ulong index)
        {
            /*
            List<BigInteger> blockTransactions;
            if (!this.blockToTransactionMapping.TryGetValue(blockIdentifier, out blockTransactions))
            {
                throw new NotSupportedException("ADD BLOCK FIRST BRO");
            }

            if (index > Int32.MaxValue)
            {
                throw new NotSupportedException("Seriously?");
            }

            this.transactionToBlockMapping[transactionIdentifier] = blockIdentifier;

            int intIndex = (int)index;

            lock (blockTransactions)
            {
                while (blockTransactions.Count < intIndex + 1)
                {
                    blockTransactions.Add(BigInteger.Zero);
                }

                blockTransactions[intIndex] = transactionIdentifier;
            }
            */
        }
    }
}
