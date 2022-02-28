using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using System.Collections.Generic;
using System.Linq;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class SkippedMessageKeyGroups
    {

        public int id
        {
            get => _id;
            set => _id = value;
        }

        private int _id;

        public readonly List<SkippedMessageKeyGroup> MKS = new List<SkippedMessageKeyGroup>();

        /// <summary>
        /// Adds the given message key (<paramref name="mk"/>) to the stored message keys.
        /// </summary>
        public void SetMessageKey(ECPubKey dhr, uint nr, byte[] mk)
        {
            SkippedMessageKeyGroup group = MKS.Where(g => g.dh.Equals(dhr)).FirstOrDefault();
            if (group is null)
            {
                group = new SkippedMessageKeyGroup(dhr);
                MKS.Add(group);
            }
            group.SetKey(nr, mk);
        }

        /// <summary>
        /// Tries to find the requested message key. If found it will be returned and removed.
        /// </summary>
        public byte[] GetMessagekey(ECPubKey dhr, uint nr)
        {
            SkippedMessageKeyGroup group = MKS.Where(g => g.dh.Equals(dhr)).FirstOrDefault();
            return group?.RemoveKey(nr)?.mk;
        }

    }
}
