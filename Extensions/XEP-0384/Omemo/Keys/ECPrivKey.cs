namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    public class ECPrivKey : ECKey
    {

        public ECPrivKey() { }

        public ECPrivKey(byte[] pubKey) : base(pubKey) { }

        /// <summary>
        /// Creates a copy of the object, not including <see cref="id"/>.
        /// </summary>
        public new ECPrivKey Clone()
        {
            return new ECPrivKey(Key);
        }

    }
}
