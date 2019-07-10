
using System;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the CallLogItemEventArgs event
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// Message received
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
