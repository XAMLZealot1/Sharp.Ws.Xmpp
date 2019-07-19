
using System;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the MessageEventArgs event
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// Message string of the event
        /// </summary>
        public String Message
        {
            get;
            private set;
        }

         public MessageEventArgs(String message)
        {
            Message = message;
        }
    }
}
