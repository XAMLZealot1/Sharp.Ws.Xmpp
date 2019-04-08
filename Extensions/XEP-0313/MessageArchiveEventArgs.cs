using System;
using Sharp.Xmpp.Im;

namespace Sharp.Xmpp.Extensions
{
    public class MessageArchiveEventArgs : EventArgs
    {
        /// <summary>
        /// Id of the query which asked for messages stored in archive
        /// </summary>
        public String QueryId
        {
            get;
            private set;
        }

        /// <summary>
        /// Message archive retrieved
        /// </summary>
        public Message Message
        {
            get;
            private set;
        }

        public MessageArchiveEventArgs(String queryId, Message message)
        {
            QueryId = queryId;
            Message = message;
        }
    }
}
