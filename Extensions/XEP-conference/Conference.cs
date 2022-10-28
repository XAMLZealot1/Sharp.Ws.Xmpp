using XMPP.Net.Im;
using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using System.Xml;


namespace XMPP.Net.Extensions
{
    /// <summary>
    /// Implements the 'Conference' extension used in Rainbow Hub
    /// </summary>
    internal class Conference : XmppExtension, IInputFilter<Message>
    {
        private static readonly ILogger log = LogFactory.CreateLogger<Conference>();

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "jabber:iq:conference" };
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
                return Extension.Conference;
            }
        }

        /// <summary>
        /// The event that is raised when a conference has been updated
        /// </summary>
         public event EventHandler<MessageEventArgs> ConferenceUpdated;

        /// <summary>
        /// Invoked when a message stanza has been received.
        /// </summary>
        /// <param name="stanza">The stanza which has been received.</param>
        /// <returns>true to intercept the stanza or false to pass the stanza
        /// on to the next handler.</returns>
        public bool Input(Message message)
        {
            if (message.Type == MessageType.Chat)
            {
                // Do we receive a conference-info message ?
                if (message.Data["conference-info"] != null)
                {
                    ConferenceUpdated.Raise(this, new MessageEventArgs(message.Data["conference-info"].ToXmlString()));
                    return true;
                }
            }

            // Pass the message on to the next handler.
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the Conference class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public Conference(XmppIm im)
            : base(im)
        {
        }
    }
}