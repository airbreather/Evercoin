using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Represents a transfer of value.
    /// </summary>
    public interface ITransaction
    {
        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        IReadOnlyList<IValueSource> Inputs { get; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        IReadOnlyList<ITransactionValueSource> Outputs { get; }
    }
}
