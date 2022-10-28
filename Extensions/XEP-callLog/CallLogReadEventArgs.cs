using System;

namespace XMPP.Net.Extensions
{
    /// <summary>
    /// Provides data for the CallLogReadEventArgs event
    /// </summary>
    public class CallLogReadEventArgs : EventArgs
    {
        /// <summary>
        /// Id of the call log item
        /// </summary>
        public String CallId
        {
            get;
            private set;
        }

        public CallLogReadEventArgs(String callId)
        {
            CallId = callId;
        }
    }
}
