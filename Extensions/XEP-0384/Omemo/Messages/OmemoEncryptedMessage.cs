using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Ws.Xmpp.Im;
using Sharp.Xmpp;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Messages
{
    internal class OmemoEncryptedMessage : Sharp.Xmpp.Im.Message
    {
        OmemoEncryption extension;

        public OmemoEncryptedMessage(Sharp.Xmpp.Im.Message message, OmemoEncryption extension): base(message)
        {
            LoadNodes(message);
            ParseNodeData();
            this.extension = extension;
        }
        public OmemoEncryptedMessage(Jid to, string body = null, string subject = null, string thread = null, MessageType type = MessageType.Normal,
            CultureInfo language = null, Dictionary<String, String> oobInfo = null) : base(to, body, subject, thread, type, language, oobInfo)
        {

        }
        public OmemoEncryptedMessage(Jid to, IDictionary<string, string> bodies,
            IDictionary<string, string> subjects = null, string thread = null,
            MessageType type = MessageType.Normal, CultureInfo language = null, Dictionary<String, String> oobInfo = null) : base(to, bodies, subjects, thread, type, language, oobInfo)
        { }

        public OmemoAuthenticatedMessage AuthenticatedMessage { get; private set; }

        public string Base64Payload { get; private set; }

        public bool Encrypted { get; private set; }

        public bool IsKEX { get; private set; }

        public List<OmemoKey> Keys { get; private set; } = new List<OmemoKey>();

        public List<OmemoKeyset> Keysets { get; private set; } = new List<OmemoKeyset>();

        public uint SID { get; private set; }

        #region Nodes

        public XmlElement EncryptedNode { get; private set; }

        public XmlElement HeaderNode { get; private set; }

        public XmlElement IvNode { get; private set; }

        public XmlElement PayloadNode { get; private set; }

        #endregion

        internal void Decrypt(Jid recipient, uint recipientDeviceID, SignedPreKey recipientPreKey, IEnumerable<PreKey> recipientPreKeys)
        {
            OmemoKeyset keyset = Keysets.FirstOrDefault(k => k.BareJid.GetBareJid().Equals(recipient.GetBareJid()));

            OmemoKey deviceKey = null;

            if (keyset != null)
                deviceKey = keyset.Keys?.FirstOrDefault(k => k.DeviceID == recipientDeviceID);
            else
                deviceKey = Keys.FirstOrDefault(k => k.DeviceID == recipientDeviceID);

            if (deviceKey == null)
                throw new Exception("Failed to decrypt message. Not encrypted for this device.");

            byte[] data = Convert.FromBase64String(deviceKey.Base64Payload);

            var senderAddress = new OmemoProtocolAddress(From, SID);

            OmemoKeyExchangeMessage keyExchange = null;

            PreKey usedPreKey = null;

            if (deviceKey.IsKex)
            {
                keyExchange = new OmemoKeyExchangeMessage(data);
                OmemoSession oldSession = extension.settings.Storage.LoadSession(senderAddress);
                if (oldSession != null && oldSession.ek.Equals(keyExchange))
                {
                    // Existing session
                    AuthenticatedMessage = keyExchange.Message;
                }
                else
                {
                    if (keyExchange.SPKID != recipientPreKey.preKey.keyId)
                        throw new Exception($"Failed to decrypt message. Signed PreKey with id {keyExchange.SPKID} not available any more.");

                    usedPreKey = recipientPreKeys.FirstOrDefault(k => k.keyId == keyExchange.SPKID);
                    if (usedPreKey == null)
                        throw new Exception($"Failed to decrypt message. PreKey with id {keyExchange.PKID} not available any more.");

                    AuthenticatedMessage = keyExchange.Message;
                }
            }
            else
            {
                AuthenticatedMessage = new OmemoAuthenticatedMessage(data);
            }

            //ValidateSender(senderAddress, keyExchange.IK);
            OmemoSession session = LoadSession(deviceKey.IsKex, extension.settings.Identity.IdentityKeyPair, extension.settings.Identity.SignedPreKey, usedPreKey, keyExchange, senderAddress);

            var ratchet = new DoubleRachet(extension.settings.Identity.IdentityKeyPair);

            if (IsKEX)
            {
                byte[] encodedContent = Convert.FromBase64String(Base64Payload);
                byte[] decodedContent = ratchet.DecryptMessage(AuthenticatedMessage, session, encodedContent);
                string content = Encoding.UTF8.GetString(decodedContent);
            }
            else
            {
                byte[] dummyKeyHmac = new DoubleRachet(extension.settings.Identity.IdentityKeyPair).DecryptKeyHmacForDevice(AuthenticatedMessage, session);
                if (!dummyKeyHmac.SequenceEqual(new byte[32]))
                {
                    throw new Exception($"Expected to decrypt 32 zero bytes from pure key exchange messages, but received: {CryptoUtils.ToHexString(dummyKeyHmac)}");
                }
            }



        }

        private void LoadNodes(Message message)
        {
            EncryptedNode = message.Data["encrypted"];
            HeaderNode = EncryptedNode["header"];
            PayloadNode = EncryptedNode["payload"];

            if (PayloadNode == null)
                IsKEX = true;
            else
                Base64Payload = PayloadNode.InnerText;

            foreach (XmlElement elem in HeaderNode.ChildNodes)
            {
                switch (elem.Name)
                {
                    case "keys":
                        Keysets.Add(new OmemoKeyset(elem));
                        break;
                    case "key":
                        Keys.Add(new OmemoKey(elem));
                        break;
                    case "iv":
                        IvNode = elem;
                        break;
                }
            }

            Encrypted = true;
        }

        private OmemoSession LoadSession(bool isKex, IdentityKeyPair RecipientID, SignedPreKey recipientSignedKey, PreKey usedPreKey, OmemoKeyExchangeMessage keyExchange, OmemoProtocolAddress senderAddress)
        {
            OmemoSession session = null;

            if (isKex)
            {
                session = new OmemoSession(RecipientID, recipientSignedKey, usedPreKey, keyExchange);
            }
            else
            {
                session = extension.settings.Storage.LoadSession(senderAddress);
                if (session is null)
                {
                    throw new InvalidOperationException("Failed to decrypt. No session found.");
                }
                //Logger.Debug("Loaded OMEMO session for decrypting: " + session.ToString());
            }

            return session;
        }

        private void ParseNodeData()
        {
            SID = uint.Parse(HeaderNode.GetAttribute("sid"));
        }

        private void ValidateSender(OmemoProtocolAddress senderAddress, ECPubKey identityKey)
        {
            OmemoFingerprint fingerprint = extension.settings.Storage.LoadFingerprint(senderAddress);
            // New device:
            if (fingerprint is null)
            {
                List<Tuple<OmemoProtocolAddress, string>> devices = extension.settings.Storage.LoadDevices(senderAddress.BareJid.ToString());
                devices.Add(new Tuple<OmemoProtocolAddress, string>(senderAddress, null));
                extension.settings.Storage.StoreDevices(devices, senderAddress.BareJid.ToString());
                fingerprint = new OmemoFingerprint(identityKey, senderAddress, DateTime.Now, false);
            }
            else
            {
                fingerprint.lastSeen = DateTime.Now;
            }

            extension.settings.Storage.StoreFingerprint(fingerprint);

            if (extension.settings.AcceptTrustedKeysOnly && !fingerprint.trusted)
            {
                throw new Exception("Failed to decrypt OMEMO message since we do not trust the sender.");
            }
        }
    }
}
