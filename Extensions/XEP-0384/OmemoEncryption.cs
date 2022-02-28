using libsignal.state;
using libsignal.util;
using Sharp.Ws.Xmpp.Extensions.Omemo;
using Sharp.Ws.Xmpp.Extensions.Omemo.Storage;
using Sharp.Xmpp;
using Sharp.Xmpp.Client;
using Sharp.Xmpp.Core;
using Sharp.Xmpp.Extensions;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions
{
    /// <summary>
    /// Implements the 'OMEMO Encryption' extension as defined in XEP-0384
    /// </summary>
    internal class OmemoEncryption : XmppExtension, IInputFilter<Sharp.Xmpp.Im.Message>, IInputFilter<Iq>, IOutputFilter<Iq>
    {
        internal const string NS_OMEMO = "urn:xmpp:omemo:2";
        internal const string NS_DEVICE_DISCOVERY = "urn:xmpp:omemo:2:devices";
        internal const string NS_BUNDLES = "urn:xmpp:omemo:2:bundles";

        /// <summary>
        /// Initializes a new instance of the ServerIpCheck class.
        /// </summary>
        /// <param name="im">A reference to the XmppIm instance on whose behalf this
        /// instance is created.</param>
        public OmemoEncryption(XmppIm im) : base(im)
        {
        }

        /// <summary>
        /// A reference to the 'Entity Capabilities' extension instance.
        /// </summary>
        private EntityCapabilities ecapa;

        /// <summary>
        /// A reference to the 'Personal Eventing Protocol' extension instance
        /// </summary>
        private Pep pep;

        /// <summary>
        /// A reference to the 'Stanza Content Encryption' extension instance
        /// </summary>
        private StanzaContentEncryption stanzaContentEncryption;

        /// <summary>
        /// A reference to the 'Service Discovery' extension instance
        /// </summary>
        private ServiceDiscovery serviceDiscovery;

        /// <summary>
        /// An enumerable collection of XMPP namespaces the extension implements.
        /// </summary>
        /// <remarks>This is used for compiling the list of supported extensions
        /// advertised by the 'Service Discovery' extension.</remarks>
        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { NS_OMEMO };
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
                return Extension.OmemoEncryption;
            }
        }

        /// <summary>
        /// Determines whether our server supports personal eventing and thusly
        /// the Omemo Encryption extension.
        /// </summary>
        public bool Supported
        {
            get
            {
                return pep.Supported;
            }
        }

        internal IRegistrationStore RegistrationStore { get; private set; }

        internal SignalProtocolStore SignalStore { get; private set; }

        /// <summary>
        /// Invoked after all extensions have been loaded.
        /// </summary>
        public override void Initialize()
        {
            ecapa = im.GetExtension<EntityCapabilities>();
            pep = im.GetExtension<Pep>();
            stanzaContentEncryption = im.GetExtension<StanzaContentEncryption>();
            pep.Subscribe("eu.siacs.conversations.axolotl.devicelist", DeviceListPublished);
        }

        internal void PublishBundle()
        {
            
        }

        internal void PublishDeviceList(uint deviceID)
        {
            XmlElement devices = Xml.Element("list", "eu.siacs.conversations.axolotl");
            XmlElement device = Xml.Element("device").Attr("id", deviceID.ToString());
            devices.Child(device);

            pep.Publish("eu.siacs.conversations.axolotl.devicelist", "current", data: devices);
            
        }

        internal void InitializeSignal(SignalProtocolStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            SignalStore = store;
        }

        internal bool InitializeRegistration(IRegistrationStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (!store.IsRegistered)
                Register(store);

            if (!store.IsRegistered)
                throw new Exception("Unable to register omemo device");

            if (store.IsRegistered)
                RegistrationStore = store;

            return store.IsRegistered;
        }

        internal void Register(IRegistrationStore store)
        {
            store.IdentityKeys = KeyHelper.generateIdentityKeyPair();
            store.RegistrationID = KeyHelper.generateRegistrationId(false);

            if (store.IsRegistered)
            {
                PublishDeviceList(store.RegistrationID);
            }
        }

        private void DeviceListPublished(Jid jid, XmlElement element)
        {

        }

        public bool Input(Sharp.Xmpp.Im.Message stanza)
        {
            // Receive encrypted OMEMO messages?

            return false;
        }

        public bool Input(Iq stanza)
        {
            // Handle device list responses?

            var pubSub = stanza.Data["pubsub"];
            var items = pubSub["items"];

            if (stanza.To.GetBareJid().Equals(im.Jid.GetBareJid()) && pubSub?.NamespaceURI == "http://jabber.org/protocol/pubsub" && items?.GetAttribute("node") == "eu.siacs.conversations.axolotl.devicelist")
            {
                return true;
            }

            return false;
        }

        public void Output(Iq stanza)
        {
            if (stanza.Data?.SelectSingleNode("//publish")?.Attributes["node"]?.Value == "eu.siacs.conversations.axolotl.devicelist")
            {

                XmlElement pubsub = stanza.Data.SelectSingleNode("//pubsub") as XmlElement;

                XmlElement publishOptions = Xml.Element("publish-options");

                XmlElement x = publishOptions.Append(Xml.Element("x", "jabber:x:data").Attr("type", "submit"));

                XmlElement f1 = Xml.Element("field").Attr("var", "FORM_TYPE").Attr("type", "hidden");
                XmlElement v1 = Xml.Element("value").InnerText("http://jabber.org/protocol/pubsub#publish-options");
                f1.Child(v1);
                x.Child(f1);

                XmlElement f2 = Xml.Element("field").Attr("var", "pubsub#persist_items");
                XmlElement v2 = Xml.Element("value").InnerText("true");
                f2.Child(v2);
                x.Child(f2);

                XmlElement f3 = Xml.Element("field").Attr("var", "pubsub#access_model");
                XmlElement v3 = Xml.Element("value").InnerText("open");
                f3.Child(v3);
                x.Child(f3);

                stanza.Data["pubsub"].Child(publishOptions);

                string xml = stanza.ToString();

            }
        }
    }
}
