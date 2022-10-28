﻿using System;
using System.Collections.Generic;
using System.Text;

namespace XMPP.Net.Extensions
{
    /// <summary>
    /// Provides data for the PbxAgentInfoEventArgs event
    /// </summary>
    public class PbxAgentInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Phone Api Status
        /// </summary>
        public String PhoneApiStatus
        {
            get;
            private set;
        }

        /// <summary>
        /// Xmpp Agent Status
        /// </summary>
        public String XmppAgentStatus
        {
            get;
            private set;
        }

        /// <summary>
        /// Version
        /// </summary>
        public String Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Features
        /// </summary>
        public String Features
        {
            get;
            private set;
        }


        public PbxAgentInfoEventArgs(String phoneApiStatus, String xmppAgentStatus, String version, String features)
        {
            PhoneApiStatus = phoneApiStatus;
            XmppAgentStatus = xmppAgentStatus;
            Version = version;
            Features = features;
        }

    }
}
