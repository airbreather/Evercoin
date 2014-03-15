namespace Evercoin
{
    /// <summary>
    /// Enumerates values that indicate which direction a connection is going.
    /// </summary>
    public enum ConnectionDirection
    {
        /// <summary>
        /// The local endpoint is initiating the connection.
        /// </summary>
        Outgoing,

        /// <summary>
        /// The remote endpoint is initiating the connection.
        /// </summary>
        Incoming
    }
}
