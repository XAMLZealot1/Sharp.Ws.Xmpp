using Sharp.Ws.Xmpp.Extensions.Omemo;
using Sharp.Ws.Xmpp.Extensions.Omemo.Messages;
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
    internal class OmemoEncryption : XmppExtension, IInputFilter<Sharp.Xmpp.Im.Message>, IInputFilter<Iq>
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

        internal OmemoEncryptionSettings settings;


        /// <summary>
        /// Invoked after all extensions have been loaded.
        /// </summary>
        public override void Initialize()
        {
            ecapa = im.GetExtension<EntityCapabilities>();
            pep = im.GetExtension<Pep>();
            stanzaContentEncryption = im.GetExtension<StanzaContentEncryption>();
            settings = im.OmemoSettings;
        }

        public bool Input(Sharp.Xmpp.Im.Message stanza)
        {
            //var encryption = stanza.Data["encryption"];

            //if (encryption?.GetAttribute("name")?.Equals("omemo", StringComparison.OrdinalIgnoreCase) ?? false)
            //{
            //    if (encryption.NamespaceURI == "urn:xmpp:eme:0")
            //    {
            //        var header = encryption["header"];
            //        var sid = uint.Parse(header?.GetAttribute("sid") ?? "0");

            //        if (sid > 0)
            //        {
            //            var iv = header["iv"];

            //            if (iv == null)
            //                return false;

            //            var keyNodes = header?.SelectNodes("key");

            //            if (keyNodes == null || keyNodes.Count == 0)
            //                return false;

            //            foreach (XmlElement keyNode in keyNodes)
            //            {
            //                bool isPreKey = keyNode.GetAttribute("prekey") == "true";
            //                if (isPreKey)
            //                {

            //                }
            //            }
            //        }
            //    }
            //}

            return false;
        }

        public bool Input(Iq stanza)
        {
            var pubSub = stanza.Data["pubsub"];
            var items = pubSub["items"];

            if (stanza.To.GetBareJid().Equals(im.Jid.GetBareJid()) && pubSub?.NamespaceURI == "http://jabber.org/protocol/pubsub" && items?.GetAttribute("node") == "eu.siacs.conversations.axolotl.devicelist")
            {
                return true;
            }

            return false;
        }
    }
}
