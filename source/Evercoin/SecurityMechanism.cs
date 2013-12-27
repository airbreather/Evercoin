using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// A value that indicates a mechanism used to keep the network secure in
    /// the face of untrusted entities.
    /// </summary>
    public enum SecurityMechanism
    {
        /// <summary>
        /// Requires an entity to submit proof that some work was done, such as
        /// solving a problem that, on average, takes many CPU cycles to solve.
        /// </summary>
        /// <remarks>
        /// The main Bitcoin network uses this exclusively at time of writing.
        /// </remarks>
        ProofOfWork,

        /// <summary>
        /// Requires an entity to prove that they have some stake in the system
        /// by submitting a transaction with a sufficiently large product of
        /// coin age and combined value.
        /// </summary>
        /// <remarks>
        /// The main Peercoin network uses this in addition to proof-of-work.
        /// </remarks>
        CoinAgeProofOfStake
    }
}
