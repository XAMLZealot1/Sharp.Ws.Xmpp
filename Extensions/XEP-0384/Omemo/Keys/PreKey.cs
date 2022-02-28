namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    public class PreKey : AbstractECKeyPair
    {

        public uint keyId
        {
            get => _keyId;
            set => _keyId = value;
        }

        private uint _keyId;

        public PreKey() { }

        public PreKey(ECPrivKey privKey, ECPubKey pubKey, uint keyId) : base(privKey, pubKey)
        {
            this.keyId = keyId;
        }

        public override bool Equals(object obj)
        {
            return obj is PreKey preKey && preKey.keyId == keyId && base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ (int)keyId;
        }

    }
}
