using System;

namespace XMPP.Net.Extensions
{
    public class RoomInvitationEventArgs : EventArgs
    {
        /// <summary>
        /// The room  id 
        /// </summary>
        public String RoomId
        {
            get;
            private set;
        }

        /// <summary>
        /// The room jid
        /// </summary>
        public String RoomJid
        {
            get;
            private set;
        }

        /// <summary>
        /// The roon name
        /// </summary>
        public String RoomName
        {
            get;
            private set;
        }


        /// <summary>
        /// The User Id who send the invitation
        /// </summary>
        public String UserId
        {
            get;
            private set;
        }

        /// <summary>
        /// The User Jid who send the invitation
        /// </summary>
        public String UserJid
        {
            get;
            private set;
        }

        /// <summary>
        /// The User Display Name who send the invitation
        /// </summary>
        public String UserDisplayName
        {
            get;
            private set;
        }

        /// <summary>
        /// The subject's invitation
        /// </summary>
        public String Subject
        {
            get;
            private set;
        }


        /// <summary>
        /// Initializes a new instance of the FavoriteManagementEventArgs class.
        /// </summary>
        /// <param name="id">the favorite id</param>
        /// <param name="action">the action about this favorite</param>
        /// <param name="type">the favorite's type</param>
        /// <param name="position">the favorite's status</param>
        /// <param name="peerId">the favorite's peerId</param>
        /// <summary>
        public RoomInvitationEventArgs(String roomId, String roomJid, String roomName, String userId, String userJid, String userDisplayName, String subject)
        {
            RoomId = roomId;
            RoomJid = roomJid;
            RoomName = roomName;
            UserId = userId;
            UserJid = userJid;
            UserDisplayName = userDisplayName;
            Subject = subject;
        }
    }
}
