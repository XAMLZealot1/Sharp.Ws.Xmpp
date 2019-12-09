using System;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the MessageDeliveryReceived event.
    /// </summary>
    public class JidEventArgs : EventArgs
    {
        /// <summary>
        /// The Jid related to this event
        /// </summary>
        public string Jid
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the JidEventArgs class.
        /// </summary>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        public JidEventArgs(string jid)
        {
            Jid = jid;
        }
    }
}
