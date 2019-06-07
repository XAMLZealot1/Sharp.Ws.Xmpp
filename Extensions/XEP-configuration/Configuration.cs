using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;

using log4net;
using System.Xml;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Implements the 'Configuration' extension used in Rainbow Hub
    /// </summary>
    internal class Configuration : XmppExtension, IInputFilter<Message>
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Configuration));

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "jabber:iq:configuration" };
            }
        }

        /// <summary>
        /// The named constant of the Extension enumeration that corresponds to this
        /// extension.
        /// </summary>
        public override Extension Xep
        {
            get
            {
                return Extension.Configuration;
            }
        }

        /// <summary>
        /// The event that is raised when an user invitation has been received
        /// </summary>
        public event EventHandler<UserInvitationEventArgs> UserInvitation;

        /// <summary>
        /// The event that is raised when an conversation has been created / updated / deleted
        /// </summary>
        public event EventHandler<ConversationManagementEventArgs> ConversationManagement;

        /// <summary>
        /// The event that is raised when an favorite has been created / updated / deleted
        /// </summary>
        public event EventHandler<FavoriteManagementEventArgs> FavoriteManagement;

        /// <summary>
        /// The event that is raised when the current user password has been updated
        /// </summary>
        public event EventHandler<EventArgs> PasswordUpdated;

        /// <summary>
        /// The event that is raised when an room has been created / updated / deleted
        /// </summary>
        public event EventHandler<RoomManagementEventArgs> RoomManagement;

        /// <summary>
        /// The event that is raised when a user is invited in a room
        /// </summary>
        public event EventHandler<RoomInvitationEventArgs> RoomInvitation;


        /// <summary>
        /// Invoked when a message stanza has been received.
        /// </summary>
        /// <param name="stanza">The stanza which has been received.</param>
        /// <returns>true to intercept the stanza or false to pass the stanza
        /// on to the next handler.</returns>
        public bool Input(Message message)
        {
            if (message.Type == MessageType.Management)
            {
                // Do we receive an user invitation ?
                if (message.Data["userinvite"] != null)
                {
                    XmlElement e = message.Data["userinvite"];

                    try
                    {
                        string invitationId = e.GetAttribute("id");
                        string action = e.GetAttribute("action"); // 'create', 'update', 'delete'
                        string type = e.GetAttribute("type"); // 'received', 'sent'
                        string status = e.GetAttribute("status"); // 'canceled', 'accepted' , 'pending'

                        UserInvitation.Raise(this, new UserInvitationEventArgs(invitationId, action, type, status));
                    }
                    catch (Exception)
                    {

                    }
                }
                // Do we receive message about conversation management
                else if (message.Data["conversation"] != null)
                {
                    XmlElement e = message.Data["conversation"];

                    string conversationID = e.GetAttribute("id");
                    string action = e.GetAttribute("action"); // 'create', 'update', 'delete'
                    Dictionary<string, string> data = new Dictionary<string, string>();

                    if (e["type"] != null)
                        data.Add("type", e["type"].InnerText);

                    if (e["peerId"] != null)
                        data.Add("peerId", e["peerId"].InnerText);

                    if (e["peer"] != null)
                        data.Add("jid_im", e["peer"].InnerText);

                    if (e["mute"] != null)
                        data.Add("mute", e["mute"].InnerText);

                    if (e["lastMessageText"] != null)
                        data.Add("lastMessageText", e["lastMessageText"].InnerText);

                    if (e["lastMessageSender"] != null)
                        data.Add("lastMessageSender", e["lastMessageSender"].InnerText);

                    if (e["lastMessageDate"] != null)
                        data.Add("lastMessageDate", e["lastMessageDate"].InnerText);

                    if (e["unreceivedMessageNumber"] != null)
                        data.Add("unreceivedMessageNumber", e["unreceivedMessageNumber"].InnerText);

                    if (e["unreadMessageNumber"] != null)
                        data.Add("unreadMessageNumber", e["unreadMessageNumber"].InnerText);

                    ConversationManagement.Raise(this, new ConversationManagementEventArgs(conversationID, action, data));
                }
                // Do we receive message about favorite management
                else if (message.Data["favorite"] != null)
                {
                    XmlElement e = message.Data["favorite"];

                    string id = e.GetAttribute("id");
                    string action = e.GetAttribute("action"); // 'create', 'update', 'delete'
                    string type = e.GetAttribute("type"); // 'user', 'room', 'bot'
                    string position = e.GetAttribute("position");
                    string peerId = e.GetAttribute("peer_id");
                    FavoriteManagement.Raise(this, new FavoriteManagementEventArgs(id, action, type, position, peerId));

                }
                // Do we receive message about userpassword management
                else if (message.Data["userpassword"] != null)
                {
                    XmlElement e = message.Data["userpassword"];
                    string action = e.GetAttribute("action"); // 'update' only ?
                    if (action == "update")
                        PasswordUpdated.Raise(this, new EventArgs());
                }
                // Do we receive message about room/bubble management
                else if (message.Data["room"] != null)
                {
                    XmlElement e = message.Data["room"];
                    string roomId = e.GetAttribute("roomid");
                    string roomJid = e.GetAttribute("roomjid");

                    string userJid = e.GetAttribute("userjid"); // Not empty if user has been accepted / invited / unsubscribed / deleted
                    string status = e.GetAttribute("status"); // Not empty if user has been accepted / invited / unsubscribed / deleted

                    string topic = e.GetAttribute("topic"); // Not empty if room updated
                    string name = e.GetAttribute("name"); // Not empty if room updated

                    string lastAvatarUpdateDate = e.GetAttribute("lastAvatarUpdateDate"); // Not empty if avatar has been updated. if deleted "null" string

                    RoomManagement.Raise(this, new RoomManagementEventArgs(roomId, roomJid, userJid, status, name, topic, lastAvatarUpdateDate));
                }
                // Since it's a Management message, we prevent next handler to parse it
                return true;
            }
            else if (message.Type == MessageType.Chat)
            {
                if (message.Data["x", "jabber:x:conference"] != null)
                {
                    XmlElement e;

                    String roomId, roomJid, roomName;
                    String userid, userjid, userdisplayname;
                    String subject;

                    roomId = roomJid = roomName = "";
                    userid = userjid = userdisplayname = "";
                    subject = "";

                    e = message.Data["x", "jabber:x:conference"];
                    roomId = e.GetAttribute("roomid");
                    roomJid = e.GetAttribute("jid");
                    roomName = e.GetAttribute("name");

                    e = message.Data["x", "jabber:x:bubble:conference:owner"];
                    if(e != null)
                    {
                        userid = e.GetAttribute("userid");
                        userjid = e.GetAttribute("jid");
                        userdisplayname = e.GetAttribute("displayname");
                    }

                    e = message.Data["subject"];
                    if (e != null)
                    {
                        subject = e.InnerText;
                    }

                    // Since it's a room invitation, we prevent next handler to parse it
                    RoomInvitation.Raise(this, new RoomInvitationEventArgs(roomId, roomJid, roomName, userid, userjid, userdisplayname, subject));

                    return true;
                }
            }

            // Pass the message on to the next handler.
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the Configuration class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public Configuration(XmppIm im)
            : base(im)
        {
        }
    }
}