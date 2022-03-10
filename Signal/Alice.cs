using libsignal;
using libsignal.ecc;
using libsignal.protocol;
using libsignal.state;
using libsignal.util;
using Sharp.Ws.Xmpp.Extensions.Omemo;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Xmpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sharp.Ws.Xmpp.Signal
{
    public class Alice : SignalUser
    {

        public Alice(string jabberID, string password) : base(new Jid(jabberID), password)
        {
            SignalUser bob = new Bob("test2@weoflibertyandfreedom.com", "");

            SessionBuilder sessionBuilder = new SessionBuilder(store, bob.Address);

            var bundle1 = bob.RequestBundle(out ECPublicKey spk, out ECPublicKey pk);

            var obundle = new OmemoBundle(bundle1, new OmemoKey[] { new OmemoKey(Convert.ToBase64String(bundle1.getPreKey().serialize()), bundle1.getPreKeyId()) });

            sessionBuilder.process(obundle.ToPreKey());

            string plainTextMessage = "Hello, Bob. This is Alice.";

            SessionCipher cipher = new SessionCipher(store, bob.Address);

            CiphertextMessage initialMessage = cipher.encrypt(Encoding.UTF8.GetBytes(plainTextMessage));

            byte[] data = initialMessage.serialize();

            string encryptedText = Convert.ToBase64String(data);


            bob.SendMessage(this, encryptedText);


        }

    }
}
