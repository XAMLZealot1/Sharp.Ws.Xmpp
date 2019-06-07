using System;

namespace Sharp.Xmpp.Extensions
{
    public class RoomManagementEventArgs : EventArgs
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
        /// UserJid (if any) accepted / invited / unsubscribed / deleted
        /// </summary>
        public String UserJid
        {
            get;
            private set;
        }

        /// <summary>
        /// User status (if any) accepted / invited / unsubscribed / deleted
        /// </summary>
        public String Status
        {
            get;
            private set;
        }

        /// <summary>
        /// Room's updated name (if any)
        /// </summary>
        public String Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Room's updated topic (if any)
        /// </summary>
        public String Topic
        {
            get;
            private set;
        }

        /// <summary>
        /// Last avatar update date (if any)
        /// </summary>
        public String LastAvatarUpdateDate
        {
            get;
            private set;
        }


        /// <summary>
        /// Initializes a new instance of the RoomManagementEventArgs class.
        /// </summary>
        /// <param name="roomId">the room id</param>
        /// <param name="roomJid">the room jid</param>
        /// <param name="userJid">the user JID</param>
        /// <param name="status">user status</param>
        /// <param name="name">the room name</param>
        /// <param name="topic">the room topic</param>
        /// <param name="lastAvatarUpdateDate">the last avatar romm update date</param>
        /// <summary>
        public RoomManagementEventArgs(String roomId, String roomJid, String userJid, String status, String name, String topic, String lastAvatarUpdateDate)
        {
            RoomId = roomId;
            RoomJid = roomJid;
            UserJid = userJid;
            Status = status;
            Name = name;
            Topic = topic;
            LastAvatarUpdateDate = lastAvatarUpdateDate;
        }
    }
}
