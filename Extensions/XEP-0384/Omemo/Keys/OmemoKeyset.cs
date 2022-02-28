using Sharp.Xmpp;
using System.Collections.Generic;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    internal class OmemoKeyset
    {
        internal OmemoKeyset(XmlNode node)
        {
            ParseNode(node as XmlElement);
        }
        internal OmemoKeyset(Jid jid, List<OmemoKey> keys)
        {

        }

        public Jid BareJid { get; private set; }

        public List<OmemoKey> Keys { get; private set; } = new List<OmemoKey>();

        private void ParseNode(XmlElement node)
        {
            string jid = node.GetAttribute("jid");
            if (!string.IsNullOrEmpty(jid))
                BareJid = new Jid(jid);

            foreach (XmlElement keyNode in node.ChildNodes)
            {
                if (keyNode.Name.Equals("key"))
                    Keys.Add(new OmemoKey(keyNode));
            }
        }

    }
}
