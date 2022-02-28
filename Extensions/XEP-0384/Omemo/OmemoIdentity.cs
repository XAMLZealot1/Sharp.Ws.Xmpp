using Sharp.Ws.Xmpp.Extensions.Omemo;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class OmemoIdentity
    {
        public OmemoIdentity(uint signedPreKeyID = 0, uint preKeyStartID = 0, uint preKeyCount = 100)
        {
            IdentityKeyPair = KeyHelper.GenerateIdentityKeyPair();
            SignedPreKey = KeyHelper.GenerateSignedPreKey(signedPreKeyID, IdentityKeyPair.privKey);
            PreKeys = KeyHelper.GeneratePreKeys(preKeyStartID, preKeyCount);
        }

        public IdentityKeyPair IdentityKeyPair { get; set; }

        public SignedPreKey SignedPreKey { get; set; }

        public List<PreKey> PreKeys { get; set; }

    }
}
