using Sharp.Xmpp.Core;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Implements Mechanism for providing MaM support
    /// </summary>
    internal class MessageArchiveManagment : XmppExtension, IInputFilter<Sharp.Xmpp.Im.Message>
    {
        /// <summary>
        /// A reference to the 'Entity Capabilities' extension instance.
        /// 
        /// Not yet supported
        /// </summary>
        private EntityCapabilities ecapa;

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:mam:1"  };
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
                return Extension.MessageArchiveManagment;
            }
        }

        /// <summary>
        /// Invoked after all extensions have been loaded.
        /// </summary>
        public override void Initialize()
        {
            ecapa = im.GetExtension<EntityCapabilities>();
        }

        /// <summary>
        /// The event that is raised when a result is donrecevied after asking list of messages archive
        /// </summary>
        public event EventHandler<MessageArchiveManagementResultEventArgs> MessageArchiveManagementResult;

        /// <summary>
        /// The event that is raised when a message arcchive has been found
        /// </summary>
        public event EventHandler<MessageArchiveEventArgs> MessageArchiveRetrieved;

        /// <summary>
        /// Invoked when a message stanza has been received.
        /// </summary>
        /// <param name="stanza">The stanza which has been received.</param>
        /// <returns>true to intercept the stanza or false to pass the stanza
        /// on to the next handler.</returns>
        public bool Input(Sharp.Xmpp.Im.Message message)
        {
            if ( (message.Data["result"] != null)
                && (message.Data["result"].NamespaceURI == "urn:xmpp:mam:1") )
            {
                String queryId = message.Data["result"].GetAttribute("queryid");

                MessageArchiveRetrieved.Raise(this, new MessageArchiveEventArgs(queryId, message));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Requests the XMPP entity with the specified JID a GET command.
        /// When the Result is received and it not not an error
        /// if fires the callback function
        /// </summary>
        /// <param name="jid">The JID of the XMPP entity to get.</param>
        /// <param name="queryId">The Id related to this query - it will be used to identify this request</param>
        /// <exception cref="ArgumentNullException">The jid parameter
        /// is null.</exception>
        /// <exception cref="NotSupportedException">The XMPP entity with
        /// the specified JID does not support the 'Ping' XMPP extension.</exception>
        /// <exception cref="XmppErrorException">The server returned an XMPP error code.
        /// Use the Error property of the XmppErrorException to obtain the specific
        /// error condition.</exception>
        /// <exception cref="XmppException">The server returned invalid data or another
        /// unspecified XMPP error occurred.</exception>
        public void RequestCustomIqAsync(Jid jid, string queryId, int max, MessageArchiveManagmentRequestDelegate callback, string before = null, string after = null)
        {
            jid.ThrowIfNull("jid");

            var xml = Xml.Element("query", "urn:xmpp:mam:1");
            xml.SetAttribute("queryid", queryId);
            var xmlParam = Xml.Element("set", "http://jabber.org/protocol/rsm");
            if ( max > 0 ) xmlParam.Child(Xml.Element("max").Text(max.ToString()));
            if (before == null)
                xmlParam.Child(Xml.Element("before"));
            else
                xmlParam.Child(Xml.Element("before").Text(before));
            if (after != null)
                xmlParam.Child(Xml.Element("after").Text(after));
            xml.Child(xmlParam);

            //The Request is Async
            im.IqRequestAsync(IqType.Set, jid, im.Jid, xml, null, (id, iq) =>
            {
                //For any reply we execute the callback
                if (iq.Type == IqType.Error)
                {
                    MessageArchiveManagementResult.Raise(this, new MessageArchiveManagementResultEventArgs());
                    return;
                }

                if (iq.Type == IqType.Result)
                {
                    string queryid = "";
                    MamResult complete = MamResult.Error;
                    int count = 0;
                    string first = "";
                    string last = "";
                    try
                    {
                        if ( (iq.Data["fin"] != null) && (iq.Data["fin"]["set"] != null) )
                        {
                            XmlElement e = iq.Data["fin"];

                            queryid = e.GetAttribute("queryid");
                            complete = (e.GetAttribute("complete") == "false") ? MamResult.InProgress : MamResult.Complete;

                            if(e["set"]["count"] != null)
                                count = Int16.Parse(e["set"]["count"].InnerText);

                            if (e["set"]["first"] != null)
                                first = e["set"]["first"].InnerText;

                            if (e["set"]["last"] != null)
                                last = e["set"]["last"].InnerText;

                            MessageArchiveManagementResult.Raise(this, new MessageArchiveManagementResultEventArgs(queryid, complete, count, first, last));
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        
                    }

                    MessageArchiveManagementResult.Raise(this, new MessageArchiveManagementResultEventArgs(queryid, MamResult.Error, count, first, last));
                }
            });
        }



        /// <summary>
        /// Initializes a new instance of the CustomIq class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public MessageArchiveManagment(XmppIm im)
            : base(im)
        {
        }
    }
}