using OMEMO.Net;

namespace XMPP.Net.Client
{
    public class XmppAccount : JabberAccount
    {
        public XmppAccount(string bareJid)
        {
            Jid = new OMEMO.Net.Jid(bareJid);
        }

        public OMEMO.Net.Jid Jid { get; set; }
    }
}
