using System;
using System.Linq;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    public class SignedPreKey
    {
        public int id
        {
            get => _id;
            set => _id = value;
        }

        private int _id;


        public PreKey preKey
        {
            get => _preKey;
            set => _preKey = value;
        }

        private PreKey _preKey;


        public byte[] signature
        {
            get => _signature;
            set => _signature = value;
        }

        private byte[] _signature;

        public SignedPreKey() { }

        public SignedPreKey(PreKey preKey, byte[] signature)
        {
            this.preKey = preKey;
            this.signature = signature;
        }

        public override bool Equals(object obj)
        {
            return obj is SignedPreKey signedPreKey && signedPreKey.preKey.Equals(preKey) && signedPreKey.signature.SequenceEqual(signature);
        }

        public override int GetHashCode()
        {
            return preKey.GetHashCode() ^ signature.GetHashCode();
        }


    }
}
