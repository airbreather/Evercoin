using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [Serializable]
    public sealed class MerkleTreeNode : IMerkleTreeNode, ISerializable
    {
        private const string SerializationName_Data = "Data";
        private const string SerializationName_LeftChild = "LeftChild";
        private const string SerializationName_RightChild = "RightChild";

        public MerkleTreeNode()
        {
        }

        public MerkleTreeNode(IMerkleTreeNode copyFrom)
            : this(copyFrom, new HashSet<IMerkleTreeNode>())
        {
        }

        private MerkleTreeNode(IMerkleTreeNode copyFrom, ISet<IMerkleTreeNode> iHateCycles)
        {
            if (!iHateCycles.Add(copyFrom))
            {
                throw new ArgumentException("I HATE CYCLES!", "copyFrom");
            }

            this.Data = copyFrom.Data;

            if (copyFrom.LeftChild != null)
            {
                this.LeftChild = new MerkleTreeNode(copyFrom.LeftChild, iHateCycles);
            }

            if (copyFrom.RightChild != null)
            {
                this.RightChild = new MerkleTreeNode(copyFrom.RightChild, iHateCycles);
            }
        }

        private MerkleTreeNode(SerializationInfo info, StreamingContext context)
        {
            this.Data = info.GetValue<byte[]>(SerializationName_Data);
            this.LeftChild = info.GetValue<MerkleTreeNode>(SerializationName_LeftChild);
            this.RightChild = info.GetValue<MerkleTreeNode>(SerializationName_RightChild);
        }

        /// <summary>
        /// Gets the data stored in this node.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets the left subtree.
        /// </summary>
        public MerkleTreeNode LeftChild { get; set; }

        /// <summary>
        /// Gets the right subtree.
        /// </summary>
        public MerkleTreeNode RightChild { get; set; }

        IMerkleTreeNode IMerkleTreeNode.LeftChild { get { return this.LeftChild; } }

        IMerkleTreeNode IMerkleTreeNode.RightChild { get { return this.RightChild; } }

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(SerializationName_Data, this.Data);
            info.AddValue(SerializationName_LeftChild, this.LeftChild);
            info.AddValue(SerializationName_RightChild, this.RightChild);
        }
    }
}
