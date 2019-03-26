using System;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Defines possible values for the information changed
    /// </summary>
    public enum VCardInfoChanged
    {
        /// <summary>
        /// Only avatar has changed
        /// </summary>
        Avatar,
        /// <summary>
        /// Data has changed not Avatar
        /// </summary>
        Data
    }

    /// <summary>
    /// Provides data for the VCardChanged event.
    /// </summary>
    [Serializable]
    public class VCardChangedEventArgs : EventArgs
    {

        /// <summary>
        /// The JID of the user or resource who changed
        /// request.
        /// </summary>
        public Jid Jid
        {
            get;
            private set;
        }

        /// <summary>
        /// To know what type of information chnaged
        /// </summary>
        public VCardInfoChanged Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the VCardChangedEventArgs class.
        /// </summary>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        public VCardChangedEventArgs(Jid jid, VCardInfoChanged type)
        {
            jid.ThrowIfNull("jid");
            Jid = jid;
            Type = type;
        }

    }

    
}