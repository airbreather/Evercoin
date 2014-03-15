using System;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolPingResponse
    {
        private readonly ulong nonce;

        private readonly Guid peerIdentifier;

        public ProtocolPingResponse(ulong nonce, Guid peerIdentifier)
        {
            this.nonce = nonce;
            this.peerIdentifier = peerIdentifier;
        }

        public ulong Nonce { get { return this.nonce; } }

        public Guid PeerIdentifier { get { return this.peerIdentifier; } }
    }
}
