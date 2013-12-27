using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// An <see cref="IValueSource"/> that comes from the block reward and all
    /// transaction fees for transactions included in the block.
    /// </summary>
    public interface ICoinbaseValueSource : IValueSource
    {
        /// <summary>
        /// Gets the coinbase data.
        /// </summary>
        byte[] CoinbaseData { get; }
    }
}
