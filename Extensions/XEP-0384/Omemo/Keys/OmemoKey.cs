using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    internal class OmemoKey
    {

        public OmemoKey(uint deviceID, object message)
        {

        }
        public OmemoKey(XmlElement node)
        {
            ParseNode(node);
        }

        public string Base64Payload { get; private set; }

        public uint DeviceID { get; private set; }

        public bool IsKex { get; private set; }

        private void ParseNode(XmlElement node)
        {
            Base64Payload = node.InnerText;

            string kexAttr = node.GetAttribute("kex");
            if (string.IsNullOrEmpty(kexAttr))
                IsKex = false;
            else
            {
                bool kex = false;
                if (bool.TryParse(kexAttr, out kex))
                    IsKex = kex;
            }

            string ridAttr = node.GetAttribute("rid");
            if(!string.IsNullOrEmpty(ridAttr))
            {
                uint rid = 0;
                if (uint.TryParse(ridAttr, out rid))
                    DeviceID = rid;
            }
        }

    }
}
