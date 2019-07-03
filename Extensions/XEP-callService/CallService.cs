using Sharp.Xmpp.Core;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Xml;

using log4net;


namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Implements the 'CallService' extension used in Rainbow Hub
    /// </summary>
    internal class CallService : XmppExtension, IInputFilter<Sharp.Xmpp.Im.Message>
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CallService));

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:pbxagent:callservice:1" };
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
                return Extension.CallService;
            }
        }

        /// <summary>
        /// The event that is raised when the call forward has been updated
        /// </summary>
        public event EventHandler<CallForwardEventArgs> CallForwardUpdated;

        /// <summary>
        /// The event that is raised when the nomadic status has been updated
        /// </summary>
        public event EventHandler<NomadicEventArgs> NomadicUpdated;
        

        /// <summary>
        /// Invoked when a message stanza has been received.
        /// </summary>
        /// <param name="stanza">The stanza which has been received.</param>
        /// <returns>true to intercept the stanza or false to pass the stanza
        /// on to the next handler.</returns>
        public bool Input(Sharp.Xmpp.Im.Message message)
        {
            if ( (message.Data["callservice"] != null)
                && (message.Data["callservice"].NamespaceURI == "urn:xmpp:pbxagent:callservice:1") )
            {
                if (message.Data["callservice"]["forwarded"] != null)
                {
                    String forwardType = message.Data["callservice"]["forwarded"].GetAttribute("forwardType");
                    String forwardTo = message.Data["callservice"]["forwarded"].GetAttribute("forwardTo");

                    CallForwardUpdated.Raise(this, new CallForwardEventArgs(forwardType, forwardTo));

                    return true;
                }
                else if (message.Data["callservice"]["nomadicStatus"] != null)
                {
                    Boolean featureActivated = message.Data["callservice"]["nomadicStatus"].GetAttribute("featureActivated").ToLower() == "true";
                    Boolean modeActivated = message.Data["callservice"]["nomadicStatus"].GetAttribute("modeActivated").ToLower() == "true";
                    Boolean makeCallInitiatorIsMain = message.Data["callservice"]["nomadicStatus"].GetAttribute("makeCallInitiatorIsMain").ToLower() == "true";
                    String destination = message.Data["callservice"]["nomadicStatus"].GetAttribute("destination");

                    NomadicUpdated.Raise(this, new NomadicEventArgs(featureActivated, modeActivated, makeCallInitiatorIsMain, destination));

                    return true;
                }
                else
                {
                    log.WarnFormat("[Input] Input not yet managed ...");
                    return true;
                }
            }

            // Pass the message to the next handler.
            return false;
        }


        /// <summary>
        /// Initializes a new instance of the CaCallServicellLog class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public CallService(XmppIm im)
            : base(im)
        {
        }
    }
}