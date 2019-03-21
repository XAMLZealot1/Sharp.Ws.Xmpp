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
        /// Invoked when a message stanza has been received.
        /// </summary>
        /// <param name="stanza">The stanza which has been received.</param>
        /// <returns>true to intercept the stanza or false to pass the stanza
        /// on to the next handler.</returns>
        public bool Input(Message message)
        {
            if(message.Type == MessageType.Management)
            {
                // Do we receive an user invitation ?
                if(message.Data["userinvite"] != null)
                {
                    XmlElement e = message.Data["userinvite"];

                    try
                    {
                        string invitationId = e.GetAttribute("id");
                        string action = e.GetAttribute("action"); // 'create', 'update', 'delete'
                        string type = e.GetAttribute("type"); // 'received', 'sent'
                        string status = e.GetAttribute("status"); // 'canceled', 'accepted' , 'pending'

                        UserInvitation.Raise(this, new UserInvitationEventArgs(invitationId, action, type, status));
                        return true;
                    }
                    catch (Exception)
                    {

                    }
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