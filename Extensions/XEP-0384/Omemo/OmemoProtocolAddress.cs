using Sharp.Xmpp;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class OmemoProtocolAddress
    {
        public Jid BareJid { get; private set; }

        public uint DeviceID { get; private set; }

        public OmemoProtocolAddress(string jid, uint deviceId) : this(new Jid(jid), deviceId)
        {
        }
        public OmemoProtocolAddress(Jid jid, uint deviceId)
        {
            BareJid = jid.IsBareJid ? jid : jid.GetBareJid();
        }

        public override bool Equals(object obj)
        {
            return obj is OmemoProtocolAddress address && address.DeviceID == DeviceID && string.Equals(address.BareJid, BareJid);
        }

        public override int GetHashCode()
        {
            return ((int)DeviceID) ^ BareJid.GetHashCode();
        }

        public override string ToString()
        {
            return BareJid.ToString() + ':' + DeviceID;
        }

    }
}
