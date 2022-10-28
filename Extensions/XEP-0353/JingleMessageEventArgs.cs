using System;

namespace XMPP.Net.Extensions
{
    public class JingleMessageEventArgs : EventArgs
    {
        public JingleMessage JingleMessage
        {
            get;
            set;
        }
    }
}
