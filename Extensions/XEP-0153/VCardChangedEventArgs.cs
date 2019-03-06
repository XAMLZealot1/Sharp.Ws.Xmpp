using System;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the VCardChanged event.
    /// </summary>
    [Serializable]
    public class VCardChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The JID of the user or resource who refused a pending subscription
        /// request.
        /// </summary>
        public Jid Jid
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the VCardChangedEventArgs class.
        /// </summary>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        public VCardChangedEventArgs(Jid jid)
        {
            jid.ThrowIfNull("jid");
            Jid = jid;
        }
    }
}