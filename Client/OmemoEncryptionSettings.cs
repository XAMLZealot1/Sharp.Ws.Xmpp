using Sharp.Ws.Xmpp.Extensions.Omemo;

namespace Sharp.Xmpp.Client
{
    public class OmemoEncryptionSettings
    {
        public bool AcceptTrustedKeysOnly { get; set; }

        public uint DeviceID { get; set; }

        public OmemoIdentity Identity { get; set; }

        public IOmemoStorage Storage { get; set; }

    }
}
