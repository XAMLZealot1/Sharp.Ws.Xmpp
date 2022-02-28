using libsignal;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Storage
{
    public interface IRegistrationStore
    {

        IdentityKeyPair IdentityKeys { get; set; }

        uint RegistrationID { get; set; }

        bool IsRegistered { get; }
    }
}
