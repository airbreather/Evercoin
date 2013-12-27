using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Represents a source of value that is available for spending.
    /// </summary>
    public interface IValueSource
    {
        /// <summary>
        /// Gets how much value can be spent by this source.
        /// </summary>
        decimal AvailableValue { get; }
    }
}
