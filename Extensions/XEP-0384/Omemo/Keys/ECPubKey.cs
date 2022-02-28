namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    public class ECPubKey : ECKey
    {
        public ECPubKey() { }

        public ECPubKey(byte[] pubKey) : base(pubKey) { }

        /// <summary>
        /// Creates a copy of the object, not including <see cref="id"/>.
        /// </summary>
        public new ECPubKey Clone()
        {
            return new ECPubKey(Key);
        }

    }
}
