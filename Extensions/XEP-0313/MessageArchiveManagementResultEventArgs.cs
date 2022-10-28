﻿using System;
using System.Collections.Generic;
using System.Text;

namespace XMPP.Net.Extensions
{

    public enum MamResult
    {
        /// <summary>
        /// An error occured when asking messages archive
        /// </summary>
        Error,

        /// <summary>
        /// We have not asked all messages archive
        /// </summary>
        InProgress,

        /// <summary>
        /// All messages archives has been asked
        /// </summary>
        Complete
    }

    /// <summary>
    /// Provides data for the MamResultEventArgs event
    /// </summary>
    public class MessageArchiveManagementResultEventArgs : EventArgs
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
        /// Id of the query which asked for messages stored in archive
        /// </summary>
        public MamResult Result
        {
            get;
            private set;
        }

        /// <summary>
        /// The count of messages in the archive
        /// </summary>
        public int Count
        {
            get;
            private set;
        }

        /// <summary>
        /// Message ID of the first archive received
        /// </summary>
        public string First
        {
            get;
            private set;
        }

        /// <summary>
        /// Message ID of the last archive received
        /// </summary>
        public string Last
        {
            get;
            private set;
        }

        public MessageArchiveManagementResultEventArgs(String queryId, MamResult result, int count, string first, string last)
        {
            QueryId = queryId;
            Result = result;
            Count = count;
            First = first;
            Last = last;
        }

        public MessageArchiveManagementResultEventArgs()
        {
            Result = MamResult.Error;
        }
    }
}
