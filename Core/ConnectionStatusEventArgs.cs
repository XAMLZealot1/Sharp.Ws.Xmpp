using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMPP.Net.Core
{
    /// <summary>
    /// Provides event about for the connection status.
    /// </summary>
    public class ConnectionStatusEventArgs : EventArgs
    {
        /// <summary>
        /// The status of the connection
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ConnectionStatusEventArgs class.
        /// </summary>
        public ConnectionStatusEventArgs(bool connected)
        { Connected = connected; }
    }
}