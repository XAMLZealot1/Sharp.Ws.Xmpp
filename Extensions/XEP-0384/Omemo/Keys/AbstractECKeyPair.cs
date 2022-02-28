namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    public abstract class AbstractECKeyPair
    {
        public int id
        {
            get => _id;
            set => _id = value;
        }

        private int _id;

        public ECPrivKey privKey
        {
            get => _privKey;
            set => _privKey = value;
        }

        private ECPrivKey _privKey;

        public ECPubKey pubKey
        {
            get => _pubKey;
            set => _pubKey = value;
        }

        private ECPubKey _pubKey;

        public AbstractECKeyPair() { }

        public AbstractECKeyPair(ECPrivKey privKey, ECPubKey pubKey)
        {
            this.privKey = privKey;
            this.pubKey = pubKey;
        }

        public override bool Equals(object obj)
        {
            return obj is AbstractECKeyPair pair && ((pair.privKey is null && privKey is null) || pair.privKey.Equals(privKey)) && ((pair.pubKey is null && pubKey is null) || pair.pubKey.Equals(pubKey));
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (!(privKey is null))
            {
                hash = privKey.GetHashCode();
            }

            if (!(privKey is null))
            {
                hash ^= pubKey.GetHashCode();
            }

            return hash;
        }


    }
}
