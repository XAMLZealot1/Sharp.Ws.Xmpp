using Sharp.Xmpp;
using Sharp.Xmpp.Core;
using Sharp.Xmpp.Omemo;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using System;
using System.Collections.Generic;
using System.Text;
using Sharp.Xmpp.Im;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Messages
{
    public class OmemoBundleMessage : Iq
    {

        public OmemoBundleMessage(uint deviceID, SignedPreKey signedPreKey, ECPubKey identityKey, IEnumerable<PreKey> preKeys) : base(IqType.Set, Guid.NewGuid().ToString(""))
        {
            DeviceID = deviceID;
            SignedPreKey = signedPreKey;
            IdentityKey = identityKey;
            PreKeys = preKeys;

            GenerateXml();
        }

        public uint DeviceID { get; set; }

        public SignedPreKey SignedPreKey { get; set; }

        public ECPubKey IdentityKey { get; set; }

        public IEnumerable<PreKey> PreKeys { get; set; }

        internal XmlElement IqElement
        {
            get
            {
                return Data as XmlElement;
            }
        }

        #region Xml Elements

        public XmlElement BundleElement { get; private set; }

        public XmlElement FieldAccessElement { get; private set; }

        public XmlElement FieldAccessValueElement { get; private set; }

        public XmlElement FieldFormTypeElement { get; private set; }

        public XmlElement FieldFormTypeValueElement { get; private set; }

        public XmlElement FieldPersistElement { get; private set; }

        public XmlElement FieldPersistValueElement { get; private set; }

        public XmlElement IdentityKeyElement { get; private set; }

        public XmlElement ItemElement { get; private set; }

        public XmlElement PreKeysElement { get; private set; }

        public XmlElement PublishElement { get; private set; }

        public XmlElement PublishOptionsElement { get; private set; }

        public XmlElement PubSubElement { get; private set; }

        public XmlElement SignedPreKeyPublicElement { get; private set; }

        public XmlElement SignedPreKeySignatureElement { get; private set; }

        public XmlElement XElement { get; private set; }

        #endregion

        protected override string RootElementName => "iq";

        private void GenerateXml()
        {
            PubSubElement = Data.Append(Xml.Element("pubsub", PublishSubscribe.XEP_0060));
            PublishElement = PubSubElement.Append(Xml.Element("publish")).Attr("node", $"eu.siacs.conversations.axolotl.bundles:{DeviceID}");
            ItemElement = PublishElement.Append(Xml.Element("item")).Attr("id", "current");
            BundleElement = ItemElement.Append(Xml.Element("bundle", "eu.siacs.conversations.axolotl"));
            SignedPreKeyPublicElement = BundleElement.Append(Xml.Element("signedPreKeyPublic")).Attr("signedPreKeyId", SignedPreKey.id.ToString()).InnerText(Convert.ToBase64String(SignedPreKey.preKey.pubKey.Key));
            SignedPreKeySignatureElement = BundleElement.Append(Xml.Element("signedPreKeySignature")).InnerText(Convert.ToBase64String(SignedPreKey.signature));
            IdentityKeyElement = BundleElement.Append(Xml.Element("identityKey")).InnerText(Convert.ToBase64String(IdentityKey.Key));
            PreKeysElement = BundleElement.Append(Xml.Element("prekeys"));

            foreach (PreKey preKey in PreKeys)
                PreKeysElement.Append(Xml.Element("preKeyPublic").InnerText(Convert.ToBase64String(preKey.pubKey.Key)).Attr("preKeyId", preKey.keyId.ToString()));

            PublishOptionsElement = PubSubElement.Append(Xml.Element("publish-options"));
            XElement = PublishOptionsElement.Append(Xml.Element("x", "jabber:x:data")).Attr("type", "submit");

            FieldFormTypeElement = XElement.Append(Xml.Element("field").Attr("var", "FORM_TYPE").Attr("type", "hidden"));
            FieldFormTypeValueElement = FieldFormTypeElement.Append(Xml.Element("value").InnerText("http://jabber.org/protocol/pubsub#publish-options"));

            var f2 = Xml.Element("field").Attr("var", "pubsub#persist_items");
            FieldPersistElement = XElement.Append(f2);

            var f3 = Xml.Element("field").Attr("var", "pubsub#access_model");
            FieldAccessElement = XElement.Append(f3);

            var v2 = Xml.Element("value").InnerText("true");
            FieldPersistValueElement = (XElement.ChildNodes[1] as XmlElement).Append(v2);

            var v3 = Xml.Element("value").InnerText("open");
            FieldAccessValueElement = (XElement.ChildNodes[2] as XmlElement).Append(v3);
        }

    }
}
