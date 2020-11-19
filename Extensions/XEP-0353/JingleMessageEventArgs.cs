using System;

namespace Sharp.Xmpp.Extensions
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
