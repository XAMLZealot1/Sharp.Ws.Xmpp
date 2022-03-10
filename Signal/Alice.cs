using libsignal;
using libsignal.protocol;
using libsignal.state;
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
            //SignalUser bob = new Bob("test2@weoflibertyandfreedom.com", "Repsol!442110");

            //SessionBuilder sessionBuilder = new SessionBuilder(store, bob.Address);

            //OmemoBundle obundle = bob.GetBundle();

            //OmemoKey key = Utils.SelectRandomItem<OmemoKey>(obundle.PreKeys);

            //PreKeyBundle bobBundle;

            ////bobBundle = bob.RequestBundle();

            //bobBundle = bob.RehydrateBundle(
            //    obundle.SignedPreKeyPublic.Base64Payload,                // Signed Pre Key Public B64
            //    obundle.SignedPreKeyPublic.PreKeyID,                     // Signed Pre Key ID
            //    Convert.ToBase64String(obundle.SignedPreKeySignature),   // Signed Pre Key Signature B64
            //    Convert.ToBase64String(obundle.IdentityKeyData),         // Identity Key B64
            //    obundle.DeviceID,                                        // Device ID
            //    key.PreKeyID,                                            // Pre Key ID
            //    key.Base64Payload);                                      // Pre Key B64

            //sessionBuilder.process(bobBundle);

            //string plainTextMessage = "Hello, Bob. This is Alice.";

            //SessionCipher cipher = new SessionCipher(store, bob.Address);

            //CiphertextMessage initialMessage = cipher.encrypt(Encoding.UTF8.GetBytes(plainTextMessage));

            //byte[] data = initialMessage.serialize();

            //string encryptedText = Convert.ToBase64String(data);


            //bob.SendMessage(this, encryptedText);

            
        }

    }
}
