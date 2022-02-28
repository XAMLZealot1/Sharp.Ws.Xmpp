using System.Collections.Generic;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    /// <summary>
    /// Represents a group of OMEMO capable devices, owned by the same owner.
    /// </summary>
    public class OmemoDeviceGroup
    {

        public readonly string BARE_JID;
        public readonly Dictionary<uint, OmemoSession> SESSIONS = new Dictionary<uint, OmemoSession>();

        public OmemoDeviceGroup(string bareJid)
        {
            BARE_JID = bareJid;
        }

    }
}
