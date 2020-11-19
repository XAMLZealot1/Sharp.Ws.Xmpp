using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using NLog;


namespace Sharp.Xmpp.Extensions
{
    internal class JingleMessageInitiation : XmppExtension, IInputFilter<Sharp.Xmpp.Im.Message>
    {
        private static readonly Logger log = LogConfigurator.GetLogger(typeof(CallLog));

        private static readonly String Namespace = "urn:xmpp:jingle-message:0";

        /// <summary>
        /// Raised when an Jingle Message Initiation has beend received
        /// </summary>
        public event EventHandler<JingleMessageEventArgs> JingleMessageInitiationReceived;

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:jingle-message" };
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
                return Extension.JingleMessageInitiation;
            }
        }

        /// <summary>
        /// Invoked when a message stanza has been received.
        /// </summary>
        /// <param name="stanza">The stanza which has been received.</param>
        /// <returns>true to intercept the stanza or false to pass the stanza
        /// on to the next handler.</returns>
        public bool Input(Sharp.Xmpp.Im.Message message)
        {
            String action = null;
            if (message.Data["propose"] != null)
                action = "propose";
            else if (message.Data["retract"] != null)
                action = "retract";
            else if (message.Data["reject"] != null)
                action = "reject";
            else if (message.Data["accept"] != null)
                action = "accept";
            else if (message.Data["proceed"] != null)
                action = "proceed";

            if(!String.IsNullOrEmpty(action))
            {
                if (message.Data[action].NamespaceURI == Namespace)
                {
                    XmlElement element = message.Data[action];

                    JingleMessageEventArgs jingleMessageEventArgs = new JingleMessageEventArgs();

                    jingleMessageEventArgs.JingleMessage = new JingleMessage();
                    jingleMessageEventArgs.JingleMessage.Id = element.GetAttribute("id");
                    jingleMessageEventArgs.JingleMessage.FromJid = message.From?.GetBareJid()?.ToString();
                    jingleMessageEventArgs.JingleMessage.FromResource = message.From?.Resource;
                    jingleMessageEventArgs.JingleMessage.ToJid = message.To?.GetBareJid()?.ToString();
                    jingleMessageEventArgs.JingleMessage.ToResource = message.To?.Resource;
                    jingleMessageEventArgs.JingleMessage.Action = action;
                    jingleMessageEventArgs.JingleMessage.UnifiedPlan = (element["unifiedplan"] != null);
                    jingleMessageEventArgs.JingleMessage.DisplayName = element.GetAttribute("displayname");

                    // Media
                    XmlNodeList nodeList = element.GetElementsByTagName("description");
                    if(nodeList != null)
                    {
                        jingleMessageEventArgs.JingleMessage.Media = new List<string>();

                        XmlElement description;
                        foreach (XmlNode node in nodeList)
                        {
                            description = node as XmlElement;
                            jingleMessageEventArgs.JingleMessage.Media.Add(description.GetAttribute("media"));
                        }
                    }
                    JingleMessageInitiationReceived.Raise(this, jingleMessageEventArgs);

                    return true;
                }
            }

            // Pass the message to the next handler.
            return false;
        }

        /// <summary>
        /// To send (answer) a Jingle Message Initiation
        /// </summary>
        /// <param name="jingleMessage">Jingle Message to send</param>
        public void Send(JingleMessage jingleMessage)
        {
            Sharp.Xmpp.Im.Message message = new Sharp.Xmpp.Im.Message(jingleMessage.ToJid);
            XmlElement e = message.Data;

            XmlElement actionElement = e.OwnerDocument.CreateElement(jingleMessage.Action, Namespace);
            e.AppendChild(actionElement);

            // Id
            actionElement.SetAttribute("id", jingleMessage.Id);

            // Unified Plan
            if ( (jingleMessage.UnifiedPlan)
                && ( (jingleMessage.Action == "propose") || (jingleMessage.Action == "proceed") ) )
            {
                XmlElement unifiedplan = e.OwnerDocument.CreateElement("unifiedplan", "urn:xmpp:jingle:apps:jsep:1");
                actionElement.AppendChild(unifiedplan);
            }

            if (jingleMessage.Action == "propose")
            {
                // Display Name
                if (!String.IsNullOrEmpty(jingleMessage.DisplayName))
                    actionElement.SetAttribute("displayname", jingleMessage.DisplayName);

                // Media
                if (jingleMessage.Media != null)
                {
                    foreach (String media in jingleMessage.Media)
                    {
                        XmlElement description = e.OwnerDocument.CreateElement("description", "urn:xmpp:jingle:apps:rtp:1");
                        description.SetAttribute("media", media);
                        actionElement.AppendChild(description);
                    }
                }
            }

            im.SendMessage(message);
        }

        /// <summary>
        /// Initializes a new instance of the CallLog class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public JingleMessageInitiation(XmppIm im)
            : base(im)
        {
        }
    }
}
