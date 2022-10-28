﻿using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Logging;

using XMPP.Net.Im;

namespace XMPP.Net.Extensions
{
    /// <summary>
    /// Implements the 'Cap' extension (Common Alert Protocol) used in Rainbow Hub
    /// </summary>
    internal class Cap : XmppExtension, IInputFilter<Im.Message>
    {
        private static readonly ILogger log = LogFactory.CreateLogger<Cap>();

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "jabber:iq:alerting" };
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
                return Extension.Cap;
            }
        }

        /// <summary>
        /// The event that is raised when a chat message is received.
        /// </summary>
        public event EventHandler<Im.MessageEventArgs> AlertMessage;

        /// <summary>
        /// Invoked when a message stanza has been received.
        /// </summary>
        /// <param name="stanza">The stanza which has been received.</param>
        /// <returns>true to intercept the stanza or false to pass the stanza
        /// on to the next handler.</returns>
        public bool Input(Im.Message message)
        {
            if (message.Type == MessageType.Headline)
            {
                if (message.Data["alert", "http://www.incident.com/cap/1.0"] != null)
                {
                    //TO DO
                    log.LogDebug("[Input] Alert message received");
                    AlertMessage.Raise(this, new Im.MessageEventArgs(message.From, message));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the Cap class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public Cap(XmppIm im)
            : base(im)
        {
        }
    }
}
