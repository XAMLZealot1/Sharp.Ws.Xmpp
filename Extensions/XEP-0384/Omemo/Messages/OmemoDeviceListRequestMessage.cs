using Sharp.Xmpp;
using Sharp.Xmpp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Messages
{
    public class OmemoDeviceListRequestMessage : Iq
    {

        public OmemoDeviceListRequestMessage(Jid acct) : base(IqType.Get, Guid.NewGuid().ToString(""))
        {
            element.SetAttribute("xmlns", "jabber:client");
            Data.Append(Xml.Element("pubsub", "http://jabber.org/protocol/pubsub")).Append(Xml.Element("items").Attr("node", "eu.siacs.conversations.axolotl.devicelist"));
        }

        protected override string RootElementName => "Iq";

        internal XmlElement Element
        {
            get
            {
                return element;
            }
        }

    }
}
