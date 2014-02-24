namespace Evercoin
{
    /// <summary>
    /// The result of a "handle network message" operation.
    /// </summary>
    public enum HandledNetworkMessageResult
    {
        /// <summary>
        /// The message was handled successfully.
        /// </summary>
        Okay,

        /// <summary>
        /// The message's command was not recognized.
        /// </summary>
        UnrecognizedCommand,

        /// <summary>
        /// The message is in the incorrect format.
        /// </summary>
        MessageInvalid,

        /// <summary>
        /// The message contains data in a correct format,
        /// but is invalid in the wider context of the system.
        /// </summary>
        ContextuallyInvalid,
    }
}
