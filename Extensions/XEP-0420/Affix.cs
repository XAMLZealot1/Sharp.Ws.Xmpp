using Sharp.Xmpp;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions
{
    internal class Affix
    {
        public Affix(Jid from, Jid to)
        {
            From = from.GetBareJid();
            To = from.GetBareJid();
            RandomPaddingContent = Omemo.Utils.GenerateRandomAlphanumericString(Omemo.Utils.GenerateRandomNumber(0, 200));
        }

        public Jid From { get; }

        public string RandomPaddingContent { get; }

        public string Timestamp { get => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"); }

        public Jid To { get; }

        public IEnumerable<XmlElement> ToElements()
        {
            var elements = new List<XmlElement>();

            elements.Add(Xml.Element("time").Attr("stamp", Timestamp));
            elements.Add(Xml.Element("to").Attr("jid", To.ToString()));
            elements.Add(Xml.Element("from").Attr("jid", From.ToString()));
            elements.Add(Xml.Element("rpad").InnerText(RandomPaddingContent));

            return elements.ToArray();
        }

    }
}
