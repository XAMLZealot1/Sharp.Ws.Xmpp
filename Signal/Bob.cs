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

            SessionBuilder sessionBuilder = new SessionBuilder(store, new SignalProtocolAddress("test1@weoflibertyandfreedom.com", 1797815022));

                        

            //sessionBuilder.process(obundle.ToPreKey());

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

            Omemo(store, bundle, message, recipient.DeviceId);
        }


        private void Omemo(SignalProtocolStore store, OmemoBundle bundle, Sharp.Xmpp.Im.Message message, uint sid)
        {
            SignalProtocolAddress address = new SignalProtocolAddress(message.To.GetBareJid().ToString(), bundle.DeviceID);
            SessionBuilder session = new SessionBuilder(store, address);
            PreKeyBundle pkb = bundle.ToPreKey();
            session.process(pkb);

            SessionCipher cipher = new SessionCipher(store, address);
            CiphertextMessage encryptedMessage = cipher.encrypt(Encoding.UTF8.GetBytes(message.Body));
            byte[] encryptedData = encryptedMessage.serialize();

            message.Body = "You received an encrypted message, but your client doesn't seem to support OMEMO.";

            message.Data.Child(Xml.Element("encryption", "urn:xmpp:eme:0").Attr("name", "OMEMO").Attr("namespace", "eu.siacs.conversations.axolotl"));

            XmlElement encryptedElement = Xml.Element("encrypted", "eu.siacs.conversations.axolotl");
            XmlElement headerElement = Xml.Element("header").Attr("sid", sid.ToString());
            XmlElement keyElement = Xml.Element("key").Attr("rid", bundle.DeviceID.ToString()).InnerText(Convert.ToBase64String(pkb.getSignedPreKey().serialize()));
            XmlElement ivElement = Xml.Element("iv").InnerText(Convert.ToBase64String(pkb.getSignedPreKeySignature()));
            XmlElement payloadElement = Xml.Element("payload").InnerText(Convert.ToBase64String(encryptedData));

            headerElement.Child(keyElement);
            headerElement.Child(ivElement);

            encryptedElement.Child(headerElement);

            message.Data.Child(encryptedElement);

            string xml = message.ToString();
        }


    }
}
