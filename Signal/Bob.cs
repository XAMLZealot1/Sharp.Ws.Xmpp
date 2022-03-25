using libsignal;
using libsignal.protocol;
using libsignal.state;
using Sharp.Ws.Xmpp.Extensions.Omemo;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Xmpp;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Sharp.Ws.Xmpp.Signal
{
    public class Bob : SignalUser
    {

        public Bob(string jabberID, string password) : base(new Jid(jabberID), password)
        {
            
        }

        public void Receive(XmlElement messageElement, XmlElement senderBundleElement, XmlElement recipientBundleElement)
        {
            OmemoBundle senderBundle = new OmemoBundle(senderBundleElement, new Jid("test2@weoflibertyandfreedom.com"));
            OmemoBundle recipientBundle = new OmemoBundle(recipientBundleElement, new Jid("test1@weoflibertyandfreedom.com"));

            OmemoMessage message = new OmemoMessage(messageElement);


        }

        public void SendFromBundle(XmlElement bundleItem)
        {
            OmemoBundle bundle = new OmemoBundle(bundleItem, "eu.siacs.conversations.axolotl.bundles:1797815022");

            PreKeyBundle preKey = bundle.ToPreKey();

            SignalProtocolAddress recipient = new SignalProtocolAddress("test1@weoflibertyandfreedom.com", 1797815022);

            SessionBuilder sessionBuilder = new SessionBuilder(store, recipient);

            sessionBuilder.process(preKey);

            string plainTextMessage = "Hello, Bob. This is Alice.";

            SessionCipher cipher = new SessionCipher(store, recipient);

            CiphertextMessage initialMessage = cipher.encrypt(Encoding.UTF8.GetBytes(plainTextMessage));

            byte[] data = initialMessage.serialize();

            string encryptedText = Convert.ToBase64String(data);

            var message = new Message(new Jid(recipient.Name), body: encryptedText) { From = jid, Id = Guid.NewGuid().ToString("") };

        }



    }
}
