using Sharp.Xmpp;
using Sharp.Xmpp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Messages
{
    public class OmemoDeviceListMessage : Iq
    {

        public OmemoDeviceListMessage(uint deviceID, Jid to, Jid from, string deviceLabel = null) : base(IqType.Set, "announce1", to, from)
        {
            DeviceID = deviceID;
        }

        public uint DeviceID { get; set; }

        public string DeviceLabel { get; set; }

        protected override string RootElementName => "iq";


        #region Xml Elements

        public XmlElement FieldAccessElement { get; private set; }

        public XmlElement FieldAccessValueElement { get; private set; }

        public XmlElement FieldFormTypeElement { get; private set; }

        public XmlElement FieldFormTypeValueElement { get; private set; }

        public XmlElement FieldPersistElement { get; private set; }

        public XmlElement FieldPersistValueElement { get; private set; }

        public XmlElement ItemElement { get; private set; }

        public XmlElement PublishElement { get; private set; }

        public XmlElement PublishOptionsElement { get; private set; }

        public XmlElement PubSubElement { get; private set; }

        public XmlElement XElement { get; private set; }

        #endregion


        private void GenerateXml()
        {
            PubSubElement = Data.Append(Xml.Element("pubsub", PublishSubscribe.XEP_0060));
            PublishElement = PubSubElement.Append(Xml.Element("publish")).Attr("node", $"eu.siacs.conversations.axolotl.bundles:{DeviceID}");
            ItemElement = PublishElement.Append(Xml.Element("item")).Attr("id", "current");

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
