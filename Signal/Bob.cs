using Sharp.Xmpp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Ws.Xmpp.Signal
{
    public class Bob : SignalUser
    {

        public Bob(string jabberID, string password) : base(new Jid(jabberID), password)
        {

        }

    }
}
