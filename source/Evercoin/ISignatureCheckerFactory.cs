using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    public interface ISignatureCheckerFactory
    {
        ISignatureChecker CreateSignatureChecker(ITransaction transaction, int outputIndex);
    }
}
