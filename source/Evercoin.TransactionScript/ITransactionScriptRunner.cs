using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.TransactionScript
{
    public interface ITransactionScriptRunner
    {
        bool EvaluateTransactionScript(IEnumerable<byte> serializedScript);
    }
}
