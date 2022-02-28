using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using System.Collections.Generic;
using System.Linq;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class SkippedMessageKeyGroup
    {


        public int id
        {
            get => _id;
            set => _id = value;
        }

        private int _id;


        public ECPubKey dh
        {
            get => _dh;
            set => _dh = value;
        }

        private ECPubKey _dh;

        // We do not need to subscribe to changed events of this hash set since it's just not interesting.

        public HashSet<SkippedMessageKeyModel> messageKeys { get; set; } = new HashSet<SkippedMessageKeyModel>();


        public SkippedMessageKeyGroup() { }

        public SkippedMessageKeyGroup(ECPubKey dh)
        {
            this.dh = dh;
        }

        public SkippedMessageKeyModel RemoveKey(uint nr)
        {
            SkippedMessageKeyModel skippedMessageKey = GetKey(nr);
            if (!(skippedMessageKey is null))
            {
                messageKeys.Remove(skippedMessageKey);
            }
            return skippedMessageKey;
        }

        public SkippedMessageKeyModel GetKey(uint nr)
        {
            return messageKeys.Where(k => k.nr == nr).FirstOrDefault();
        }

        public void SetKey(uint nr, byte[] mk)
        {
            SkippedMessageKeyModel skippedMessageKey = GetKey(nr);
            if (skippedMessageKey is null)
            {
                messageKeys.Add(new SkippedMessageKeyModel(nr, mk));
            }
            else
            {
                skippedMessageKey.mk = mk;
            }
        }

    }
}
