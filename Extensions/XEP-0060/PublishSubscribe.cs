using Sharp.Xmpp.Extensions;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Ws.Xmpp.Extensions
{
    internal class PublishSubscribe : XmppExtension
    {
        internal const string XEP_0060 = "http://jabber.org/protocol/pubsub";
       

        public PublishSubscribe(XmppIm im) : base(im)
        {

        }

        public override IEnumerable<string> Namespaces => throw new NotImplementedException();

        public override Extension Xep => throw new NotImplementedException();


    }
}
