using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [DataContract(Name = "MerkleTreeNode", Namespace = "Evercoin.Storage.Model")]
    public sealed class MerkleTreeNode : IMerkleTreeNode
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

        /// <summary>
        /// Gets the data stored in this node.
        /// </summary>
        [DataMember(Name = SerializationName_Data)]
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets the left subtree.
        /// </summary>
        [DataMember(Name = SerializationName_LeftChild)]
        public MerkleTreeNode LeftChild { get; set; }

        /// <summary>
        /// Gets the right subtree.
        /// </summary>
        [DataMember(Name = SerializationName_RightChild)]
        public MerkleTreeNode RightChild { get; set; }

        IMerkleTreeNode IMerkleTreeNode.LeftChild { get { return this.LeftChild; } }

        IMerkleTreeNode IMerkleTreeNode.RightChild { get { return this.RightChild; } }
    }
}
