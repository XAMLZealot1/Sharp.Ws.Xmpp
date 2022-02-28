namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    public class GenericECKeyPair : AbstractECKeyPair
    {
        public GenericECKeyPair() { }

        public GenericECKeyPair(ECPrivKey privKey, ECPubKey pubKey) : base(privKey, pubKey) { }

    }
}
