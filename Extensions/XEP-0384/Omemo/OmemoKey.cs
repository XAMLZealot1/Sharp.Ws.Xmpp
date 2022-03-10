using libsignal.state;
using System;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    public class OmemoKey
    {
        public OmemoKey(string base64Payload, uint preKeyID)
        {
            Base64Payload = base64Payload;
            KeyData = Convert.FromBase64String(base64Payload);
            PreKeyID = preKeyID;
        }
        public OmemoKey(PreKeyRecord preKey)
        {
            if (preKey == null)
                return;

            PreKeyID = preKey.getId();
            KeyData = preKey.getKeyPair().getPublicKey().serialize();
            Base64Payload = Convert.ToBase64String(KeyData);
        }
        public OmemoKey(XmlElement node)
        {
            ParseNode(node);
        }

        public string Base64Payload { get; private set; }

        public byte[] KeyData { get; private set; }

        public uint PreKeyID { get; private set; }

        private void ParseNode(XmlElement node)
        {
            Base64Payload = node.InnerText;
            if (uint.TryParse(node.GetAttribute("preKeyId"), out uint preKeyId))
                PreKeyID = preKeyId;

            if (!string.IsNullOrEmpty(Base64Payload))
                KeyData = Convert.FromBase64String(Base64Payload);
        }

    }
}
