using libsignal.state;
using libsignal.util;
using Microsoft.Extensions.Logging;
using Sharp.Ws.Xmpp.Extensions.Omemo;
using Sharp.Ws.Xmpp.Extensions.Omemo.Storage;
using Sharp.Xmpp;
using Sharp.Xmpp.Client;
using Sharp.Xmpp.Core;
using Sharp.Xmpp.Extensions;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions
{

    /// <summary>
    /// Implements the 'OMEMO Encryption' extension as defined in XEP-0384
    /// </summary>
    internal class OmemoEncryption : XmppExtension, IInputFilter<Stanza>, IOutputFilter<Stanza>, IOutputFilter<Iq>
    {
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
                return new string[] {
                    "urn:xmpp:omemo:2",
                    "urn:xmpp:omemo:2:bundles+notify",
                    "urn:xmpp:omemo:2:devices+notify",
                    "eu.siacs.conversations.axolotl",
                    "eu.siacs.conversations.axolotl:bundles+notify",
                    "eu.siacs.conversations.axolotldevices+notify" };
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

        internal void GetDeviceList(Jid jid = null)
        {
            try
            {
                var result = pep.RetrieveItems(im.Jid.GetBareJid(), "urn:xmpp:omemo:2:devices");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            //pep.Subscribe("urn:xmpp:omemo:2:devices", (j, e) =>
            //{

            //});
        }

        private void OnConnected(object sender, ConnectionStatusEventArgs e)
        {
            if (!e.Connected)
                return;

            PublishDeviceList(RegistrationStore.RegistrationID);
        }

        /// <summary>
        /// Invoked after all extensions have been loaded.
        /// </summary>
        public override void Initialize()
        {
            logger.Info($"Initializing Omemo extension (XEP-0384)...{Environment.NewLine}");
            ecapa = im.GetExtension<EntityCapabilities>();
            pep = im.GetExtension<Pep>();
            stanzaContentEncryption = im.GetExtension<StanzaContentEncryption>();

            if (im.Connected)
                OnConnected(im, new ConnectionStatusEventArgs(true));
            else
                im.ConnectionStatus += OnConnected;
        }

        internal void PublishBundle()
        {
            XmlElement bundle = Xml.Element("bundle", "eu.siacs.conversations.axolotl");

            var spk = SignalStore.LoadSignedPreKeys().FirstOrDefault();

            XmlElement signedPreKeyPublic = Xml.Element("signedPreKeyPublic").Attr("signedPreKeyId", spk.getId().ToString());

            pep.Publish($"eu.siacs.conversations.axolotl.bundles:{RegistrationStore.RegistrationID}", "current");
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

        public void Output(Iq stanza)
        {
            if (stanza.Data?.SelectSingleNode("//publish")?.Attributes["node"]?.Value == "eu.siacs.conversations.axolotl.devicelist")
                stanza.OpenAccessModel();
        }

        public bool Input(Stanza stanza)
        {
            logger.Debug(stanza.ToString());
            return false;
        }

        public void Output(Stanza stanza)
        {
            logger.Debug(stanza.ToString());
        }
    }
}
