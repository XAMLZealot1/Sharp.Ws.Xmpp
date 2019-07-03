using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the CallLogItemEventArgs event
    /// </summary>
    public class CallForwardEventArgs : EventArgs
    {
        /// <summary>
        /// Type of the forward: "Activation" / "Deactivation" 
        /// </summary>
        public String Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Forward to: "VOICEMAILBOX" / a phone number
        /// </summary>
        public String To
        {
            get;
            private set;
        }

 
        public CallForwardEventArgs(String type, String to)
        {
            Type = type;
            To = to;
        }

    }
}
