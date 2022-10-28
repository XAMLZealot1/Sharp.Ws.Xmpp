using System;
using XMPP.Net.Im;

namespace XMPP.Net.Extensions
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
        /// Id of the result found
        /// </summary>
        public String ResultId
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

        public MessageArchiveEventArgs(String queryId, String resultId, Message message)
        {
            QueryId = queryId;
            ResultId = resultId;
            Message = message;
        }
    }
}
