using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// The version of a transaction.
    /// </summary>
    public enum TransactionVersion : uint
    {
        /// <summary>
        /// The initial version used in transactions.
        /// </summary>
        Version1 = 1
    }
}
