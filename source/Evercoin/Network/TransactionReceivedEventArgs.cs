using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.Network
{
    /// <summary>
    /// Represents the arguments for an event raised when some
    /// <see cref="ITransaction"/> object has been received.
    /// </summary>
    public sealed class TransactionReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="transaction">
        /// The <see cref="ITransaction"/> object that has been received.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="transaction"/> is <c>null</c>.
        /// </exception>
        public TransactionReceivedEventArgs(ITransaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            this.Transaction = transaction;
        }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> object that has been received.
        /// </summary>
        public ITransaction Transaction { get; private set; }
    }
}
