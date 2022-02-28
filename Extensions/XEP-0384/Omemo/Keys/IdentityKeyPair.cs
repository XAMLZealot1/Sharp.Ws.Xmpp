using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    /// <summary>
    /// Represents a Ed25519 key pair.
    /// </summary>
    public class IdentityKeyPair : AbstractECKeyPair
    {
        public IdentityKeyPair() { }

        public IdentityKeyPair(ECPrivKey privKey, ECPubKey pubKey) : base(privKey, pubKey) { }

        /// <summary>
        /// Creates a copy of the object, not including <see cref="id"/>.
        /// </summary>
        public IdentityKeyPair Clone()
        {
            return new IdentityKeyPair(privKey.Clone(), pubKey.Clone());
        }

    }
}
