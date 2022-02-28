using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using System;
using System.Linq;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class OmemoFingerprint
    {
        public readonly ECPubKey IDENTITY_KEY;
        public readonly OmemoProtocolAddress ADDRESS;
        public DateTime lastSeen;
        public bool trusted;

        public OmemoFingerprint(ECPubKey identityKey, OmemoProtocolAddress address, DateTime lastSeen, bool trusted)
        {
            IDENTITY_KEY = identityKey;
            ADDRESS = address;
            this.lastSeen = lastSeen;
            this.trusted = trusted;
        }

        public OmemoFingerprint(ECPubKey identityKey, OmemoProtocolAddress address) : this(identityKey, address, DateTime.MinValue, false) { }

        public bool checkIdentityKey(ECPubKey other)
        {
            return other.Key.SequenceEqual(IDENTITY_KEY.Key);
        }

    }
}
