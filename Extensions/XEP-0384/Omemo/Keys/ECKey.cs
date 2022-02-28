using System;
using System.Linq;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    public class ECKey
    {

        public int ID { get; set; }

        public byte[] Key { get; set; }

        public ECKey() { }

        public ECKey(byte[] key)
        {
            Key = key;
        }

        public override bool Equals(object obj)
        {
            return obj is ECKey ecKey && ecKey.Key.SequenceEqual(Key);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        /// <summary>
        /// Creates a copy of the object, not including <see cref="id"/>.
        /// </summary>
        public ECKey Clone()
        {
            return new ECKey(Key);
        }


    }
}
