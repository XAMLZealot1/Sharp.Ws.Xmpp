using Sharp.Xmpp;
using Sharp.Xmpp.Core;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sharp.Ws.Xmpp.Extensions
{
    internal class EncryptedMessage : Sharp.Xmpp.Im.Message
    {
        public EncryptedMessage(Sharp.Xmpp.Core.Message message) : base(message)
        {
        }
        public EncryptedMessage(
            Jid to, 
            string body = null, 
            string subject = null, 
            string thread = null, 
            MessageType type = MessageType.Normal, 
            CultureInfo language = null,
            Dictionary<string, string> oobInfo = null) : base(to, body, subject, thread, type, language, oobInfo)
        {

        }

        public bool Verify()
        {
            return true;
        }
    }
}
