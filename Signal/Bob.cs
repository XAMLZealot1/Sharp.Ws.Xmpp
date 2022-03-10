using Sharp.Xmpp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Ws.Xmpp.Signal
{
    internal class Bob : SignalUser
    {

        internal Bob(string jabberID, string password) : base(new Jid(jabberID), password)
        {

        }

    }
}
