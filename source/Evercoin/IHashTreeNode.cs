using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// An object that can be used as a node in a hash tree.
    /// </summary>
    public interface IHashTreeNode
    {
        /// <summary>
        /// Gets the <see cref="IHashTreeNode"/> children in order.
        /// </summary>
        ICollection<IHashTreeNode> OrderedChildren { get; }

        /// <summary>
        /// Calculates the hash of this <see cref="IHashTreeNode"/> using a given <see cref="IHashAlgorithm"/>.
        /// </summary>
        /// <param name="hashAlgorithm">
        /// The <see cref="IHashAlgorithm"/> to use to calculate the hash.
        /// </param>
        /// <returns>
        /// A <see cref="Stream"/> containing the hash of this <see cref="IHashTreeNode"/>.
        /// </returns>
        Stream CalculateHash(IHashAlgorithm hashAlgorithm);
    }
}
