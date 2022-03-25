using Javax.Crypto.Spec;
using libsignal;
using libsignal.protocol;
using libsignal.state;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Xmpp;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class OmemoMessage : Sharp.Xmpp.Core.Message
    {
        public string Body { get; set; }

        public string Encryption { get; private set; }

        public string ID { get; set; }

        public string IVEncoded { get; set; }

        public IList<OmemoKey> Keys { get; set; } = new List<OmemoKey>();

        public string Payload { get; set; }

        public Jid Recipient { get; set; }

        public Jid Sender { get; set; }

        public uint SenderDeviceID { get; set; }

        public string Type { get; set; }

        public OmemoMessage(SignalProtocolStore store, Message message, OmemoBundle senderBundle, params OmemoBundle[] recipientBundles)
        {
            GenerateElements(store, message, senderBundle, recipientBundles);
        }
        public OmemoMessage(XmlElement e)
        {
            ParseElement(e);
            Extract();
        }

        public void Decrypt(uint rid, SignalProtocolStore store)
        {
            OmemoKey recipientKey = Keys.FirstOrDefault(x => x.RecipientID == rid);

            if (recipientKey == null)
                throw new Exception("Message not encrypted for this device");

            SessionCipher sessionCipher = new SessionCipher(store, new SignalProtocolAddress(Sender.GetBareJid().ToString(), SenderDeviceID));

            var plainTextData = ProcessKey(recipientKey, sessionCipher);

            if (plainTextData.Length < 32)
                throw new Exception("Key did not contain auth tag. Sender needs to update their OMEMO client");

            byte[] cipherText = Convert.FromBase64String(Payload);
            int authTagLength = plainTextData.Length - 16;

            byte[] newCipherText = new byte[plainTextData.Length - 16 + cipherText.Length];
            byte[] newKey = new byte[16];

            Array.Copy(cipherText, 0, newCipherText, 0, cipherText.Length);
            Array.Copy(plainTextData, 16, newCipherText, cipherText.Length, authTagLength);
            Array.Copy(plainTextData, 0, newKey, 0, newKey.Length);

            using (RijndaelManaged crypto = new RijndaelManaged())
            {
                var decryptor = crypto.CreateDecryptor(newKey, Convert.FromBase64String(IVEncoded));
                plainTextData = decryptor.TransformFinalBlock(newCipherText, 0, newCipherText.Length);
            }

            Body = Encoding.UTF8.GetString(plainTextData);
        }

        private void Encrypt(SignalProtocolStore store, OmemoBundle[] recipientBundles)
        {
            #region Encrypt Payload

            byte[] payloadData = null;

            using (RijndaelManaged crypto = new RijndaelManaged())
            {
                crypto.GenerateKey();
                byte[] key = crypto.Key;

                crypto.GenerateIV();
                byte[] iv = crypto.IV;

                IVEncoded = Convert.ToBase64String(iv);

                ICryptoTransform encryptor = crypto.CreateEncryptor(key, iv);

                byte[] payloadBytes = null; // Encoding.UTF8.GetBytes(input);

                var cipherText = encryptor.TransformFinalBlock(payloadBytes, 0, payloadBytes.Length);

                var combinedKeyAuth = new byte[16 + 16];

                payloadData = new byte[cipherText.Length - 16];

                Array.Copy(cipherText, 0, payloadData, 0, payloadData.Length);
                Array.Copy(cipherText, cipherText.Length, combinedKeyAuth, 16, 16);
                Array.Copy(key, 0, combinedKeyAuth, 0, key.Length);

                payloadData = cipherText;
            }

            Payload = Convert.ToBase64String(payloadData);

            #endregion

            #region Recipient Messages

            foreach (var recipientBundle in recipientBundles)
            {
                SignalProtocolAddress recipientAddress = new SignalProtocolAddress(recipientBundle.JabberID.ToString(), recipientBundle.DeviceID);
                SessionBuilder sessionBuilder = new SessionBuilder(store, recipientAddress);
                sessionBuilder.process(recipientBundle.ToPreKey());
                SessionCipher cipher = new SessionCipher(store, recipientAddress);
                CiphertextMessage outgoingMessage = cipher.encrypt(Encoding.UTF8.GetBytes(Body));
            }

            #endregion
        }

        private string EncryptKeyPayload(SignalProtocolStore store, string plainTextMessage, OmemoBundle recipientBundle)
        {
            SignalProtocolAddress recipientAddress = new SignalProtocolAddress(recipientBundle.JabberID.ToString(), recipientBundle.DeviceID);
            SessionBuilder sessionBuilder = new SessionBuilder(store, recipientAddress);
            sessionBuilder.process(recipientBundle.ToPreKey());
            SessionCipher cipher = new SessionCipher(store, recipientAddress);
            CiphertextMessage outgoingMessage = cipher.encrypt(Encoding.UTF8.GetBytes(plainTextMessage));

            SecretKeySpec sks = new SecretKeySpec(null, "AES");

            return Convert.ToBase64String(outgoingMessage.serialize());
        }

        private void Extract()
        {
            Body = BodyElement?.InnerText;
            Encryption = EncryptionElement?.GetAttribute("name");
            ID = MessageElement?.GetAttribute("id");
            IVEncoded = IVElement?.InnerText;
            Keys = KeyElements?.Select(x => new OmemoKey(x)).ToList();
            Payload = PayloadElement?.InnerText;
            Recipient = new Jid(MessageElement?.GetAttribute("to"));
            Sender = new Jid(MessageElement?.GetAttribute("from"));
            SenderDeviceID = uint.Parse(HeaderElement?.GetAttribute("sid") ?? "0");
            Type = MessageElement?.GetAttribute("type");
        }

        private void GenerateElements(SignalProtocolStore store, Message message, OmemoBundle senderBundle, params OmemoBundle[] recipientBundles)
        {
            MessageElement = Xml.Element("message", "jabber:client")
                .Attr("to", message.To.ToString())
                .Attr("from", message.From.ToString())
                .Attr("type", MessageType.Normal.ToString())
                .Attr("id", message.Id);

            BodyElement = Xml.Element("body")
                .InnerText("You received a message encrypted with OMEMO but your client doesn't support OMEMO.");
            
            EncryptionElement = Xml.Element("encryption", "urn:xmpp:eme:0")
                .Attr("name", "OMEMO")
                .Attr("namespace", "eu.siacs.conversations.axolotl");

            EncryptedElement = Xml.Element("encrypted", "eu.siacs.conversations.axolotl");

            HeaderElement = Xml.Element("header").Attr("sid", senderBundle.DeviceID.ToString());

            //PayloadElement = Xml.Element("payload").InnerText(EncryptMessagePayload();

            IVElement = Xml.Element("iv");

            foreach (var recipientBundle in recipientBundles)
            {
                XmlElement keyElement = Xml.Element("key").InnerText(EncryptKeyPayload(store, message.Body, recipientBundle));
                HeaderElement.Child(keyElement);
                KeyElements.Add(keyElement);
            }

            HeaderElement.Child(IVElement);
            EncryptedElement.Child(HeaderElement);
            EncryptedElement.Child(PayloadElement);

            MessageElement.Child(EncryptedElement);
            MessageElement.Child(EncryptionElement);
            MessageElement.Child(BodyElement);
        }

        private void ParseElement(XmlElement e)
        {
            MessageElement = e;

            BodyElement = MessageElement["body"];
            EncryptedElement = MessageElement["encrypted"];
            EncryptionElement = MessageElement["encryption"];

            if (EncryptedElement != null)
            {
                HeaderElement = EncryptedElement["header"];
                IVElement = HeaderElement["iv"];
                PayloadElement = EncryptedElement["payload"];

                foreach (XmlElement elem in HeaderElement.ChildNodes)
                {
                    if (elem.Name == "key")
                        KeyElements.Add(elem);
                }
            }
        }

        private byte[] ProcessKey(OmemoKey key, SessionCipher cipher)
        {
            byte[] result = null;

            try
            {
                if (key.IsPreKey)
                {
                    PreKeySignalMessage preKeySignalMessage = new PreKeySignalMessage(key.KeyData);
                    IdentityKey identityKey = preKeySignalMessage.getIdentityKey();
                    Strilanc.Value.May<uint> optionalPreKeyId = preKeySignalMessage.getPreKeyId();

                    if (!optionalPreKeyId.HasValue)
                        throw new Exception("PreKey Message did not contain a pre key ID");

                    uint preKeyId = (uint)optionalPreKeyId;

                    result = cipher.decrypt(preKeySignalMessage);
                }
                else
                {
                    SignalMessage signalMessage = new SignalMessage(key.KeyData);

                    result = cipher.decrypt(signalMessage);
                }
            }
            catch (Exception ex)
            {
                // Log the shit
                throw new Exception("Error decrypting OMEMO message", ex);
            }

            return result;
        }

        #region XML

        public XmlElement BodyElement { get; set; }

        public XmlElement EncryptedElement { get; set; }

        public XmlElement EncryptionElement { get; set; }

        public XmlElement HeaderElement { get; set; }

        public XmlElement IVElement { get; set; }

        public IList<XmlElement> KeyElements { get; set; } = new List<XmlElement>();

        public XmlElement MessageElement { get; set; }

        public XmlElement PayloadElement { get; set; }

        #endregion

        protected override string RootElementName => "message";
    }
}
