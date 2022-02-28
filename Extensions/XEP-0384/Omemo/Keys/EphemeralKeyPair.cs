namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    /// <summary>
    /// Represents a Ed25519 key pair.
    /// </summary>
    public class EphemeralKeyPair : AbstractECKeyPair
    {
        public EphemeralKeyPair() { }

        public EphemeralKeyPair(ECPrivKey privKey, ECPubKey pubKey) : base(privKey, pubKey) { }

    }
}
