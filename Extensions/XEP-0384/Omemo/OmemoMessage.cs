using libsignal;
using libsignal.state;
using Sharp.Ws.Xmpp.Extensions.Omemo;
using Sharp.Xmpp;
using Sharp.Xmpp.Core;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    internal class OmemoMessage : Sharp.Xmpp.Im.Message
    {
        public OmemoMessage(Sharp.Xmpp.Core.Message message) : base(message)
        {
        }

        public void Encrypt(OmemoDevice receiverDevice, SignalProtocolStore store)
        {
            SessionBuilder session = new SessionBuilder(store, receiverDevice.Address);
            
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
