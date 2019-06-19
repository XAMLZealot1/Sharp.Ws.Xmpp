using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the CallLogItemEventArgs event
    /// </summary>
    public class CallLogItemEventArgs : EventArgs
    {
        /// <summary>
        /// Id of the call log item
        /// </summary>
        public String Id
        {
            get;
            private set;
        }

        /// <summary>
        /// Id of the call
        /// </summary>
        public String CallId
        {
            get;
            private set;
        }

        /// <summary>
        /// State of the call
        /// </summary>
        public String State
        {
            get;
            private set;
        }

        /// <summary>
        /// Callee
        /// </summary>
        public String Callee
        {
            get;
            private set;
        }

        /// <summary>
        /// Caller
        /// </summary>
        public String Caller
        {
            get;
            private set;
        }

        /// <summary>
        /// Media of the call
        /// </summary>
        public String Media
        {
            get;
            private set;
        }

        /// <summary>
        /// The time at which the call was originally started.
        /// </summary>
        public DateTime TimeStamp
        {
            get;
            private set;
        }

        public CallLogItemEventArgs(String id, String callId, String state, String callee, String caller, String media, String timeStamp)
        {
            Id = id;
            CallId = callId;
            State = state;
            Callee = callee;
            Caller = caller;
            Media = media;

            DateTime result;
            if (!DateTime.TryParse(timeStamp, out result))
                result = DateTime.Now;
            TimeStamp = result;
        }

    }
}
