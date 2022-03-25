using libsignal;
using libsignal.ecc;
using libsignal.protocol;
using libsignal.state;
using Org.BouncyCastle.Security;
using Sharp.Ws.Xmpp.Extensions.Interfaces;
using Sharp.Ws.Xmpp.Extensions.Omemo;
using Sharp.Xmpp;
using Sharp.Xmpp.Core;
using Sharp.Xmpp.Extensions;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions
{

    /// <summary>
    /// Implements the 'OMEMO Encryption' extension as defined in XEP-0384
    /// </summary>
    internal class OmemoEncryption : XmppExtension, IOutputFilter<Iq>
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
                    "eu.siacs.conversations.axolotl:devices+notify" };
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

        internal IEnumerable<PreKeyRecord> GeneratePreKeys(SignalProtocolStore signalStore, int count = 100)
        {
            int preKeyStart = new Random().Next(0, int.MaxValue - 100);
            int preKeyEnd = preKeyStart + 100;

            var result = new List<PreKeyRecord>();

            for (int i = preKeyStart; i <= preKeyEnd;i++)
            {
                uint preKeyID = Convert.ToUInt32(i);
                var preKey = new PreKeyRecord(preKeyID, Curve.generateKeyPair());
                signalStore.StorePreKey(preKeyID, preKey);
                result.Add(preKey);
            }

            return result;
        }

        internal OmemoBundle GetBundle(Jid jid, uint deviceID)
        {
            var nodes = serviceDiscovery.GetItems(jid.GetBareJid());

            var bundleItems = nodes.Where(x => Regex.IsMatch(x.Node, OmemoBundle.BundleNodePattern));
            if (bundleItems == null)
                return null;

            var result = new List<OmemoBundle>();

            foreach (var bundleItem in bundleItems)
            {
                IEnumerable<XmlElement> bundleElements = pep.RetrieveItems(bundleItem.Jid, bundleItem.Node);
                if (bundleElements == null)
                    return null;

                foreach (var bundleElement in bundleElements)
                    result.Add(new OmemoBundle(bundleElement, bundleItem.Node));
            }

            return result.FirstOrDefault(x => x.DeviceID == deviceID);
        }

        internal IEnumerable<OmemoDevice> GetDeviceList(Jid jid)
        {
            try
            {
                var nodes = serviceDiscovery.GetItems(jid.GetBareJid());

                var deviceNodes = nodes.Where(x => new string[]
                {
                    "urn:xmpp:omemo:2:devices",
                    "eu.siacs.conversations.axolotl.devicelist"
                }.Contains(x.Node));

                List<OmemoDevice> devices = new List<OmemoDevice>();
                foreach (var node in deviceNodes?.ToArray())
                {
                    var items = pep.RetrieveItems(node.Jid, node.Node);
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            var deviceList = new DeviceList(item, jid);
                            foreach (var device in deviceList?.Devices)
                            {
                                if (!devices.Any(x => x.DeviceID == device.DeviceID))
                                {
                                    device.Bundle = GetBundle(jid, device.DeviceID);
                                    devices.Add(device);
                                }
                            }
                        }
                    }
                }
                return devices;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            return new OmemoDevice[] { };
        }

        private PreKeyBundle GeneratePreKey(SignalProtocolStore store, uint deviceID, IPreKeyCollection preKeyStore)
        {
            ECKeyPair preKeyPair = Curve.generateKeyPair();
            ECKeyPair signedPreKeyPair = Curve.generateKeyPair();
            byte[] signedPreKeySignature = Curve.calculateSignature(store.GetIdentityKeyPair().getPrivateKey(), signedPreKeyPair.getPublicKey().serialize());

            uint preKeyId = preKeyStore.GetRandomPreKeyID();
            uint signedPreKeyId = Utils.GenerateRandomUint();

            var preKey = new PreKeyBundle(
                store.GetLocalRegistrationId(),
                deviceID, preKeyId, preKeyPair.getPublicKey(), signedPreKeyId,
                signedPreKeyPair.getPublicKey(), signedPreKeySignature,
                store.GetIdentityKeyPair().getPublicKey());

            store.StorePreKey(preKeyId, new PreKeyRecord(preKey.getPreKeyId(), preKeyPair));
            store.StoreSignedPreKey(signedPreKeyId, new SignedPreKeyRecord(signedPreKeyId, 0, signedPreKeyPair, signedPreKeySignature));

            return preKey;
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
            serviceDiscovery = im.GetExtension<ServiceDiscovery>();
        }

        internal OmemoBundle PublishBundle(SignalProtocolStore store, uint deviceID, IPreKeyCollection preKeyStore)
        {
            PreKeyBundle preKeyBundle = GeneratePreKey(store, deviceID, preKeyStore);
            IEnumerable<PreKeyRecord> preKeyRecords = preKeyStore.Store.Select(x => new PreKeyRecord(x.Value));
            OmemoBundle bundle = new OmemoBundle(preKeyBundle, preKeyRecords);
            im.IqRequestAsync(IqType.Set, data: bundle.RootElement);
            return bundle;
        }

        internal OmemoDevice PublishDeviceList(uint deviceID, Jid jid)
        {
            XmlElement devices = Xml.Element("list", "eu.siacs.conversations.axolotl");
            XmlElement device = Xml.Element("device").Attr("id", deviceID.ToString());
            devices.Child(device);

            pep.Publish("eu.siacs.conversations.axolotl.devicelist", "current", data: devices);

            return new OmemoDevice(device, jid);
        }

        internal void SendMessage(SignalProtocolStore store, OmemoBundle bundle, Sharp.Xmpp.Im.Message message, uint sid)
        {
            SignalProtocolAddress address = new SignalProtocolAddress(message.To.GetBareJid().ToString(), bundle.DeviceID);
            SessionBuilder session = new SessionBuilder(store, address);
            PreKeyBundle pkb = bundle.ToPreKey();
            session.process(pkb);

            SessionCipher cipher = new SessionCipher(store, address);
            CiphertextMessage encryptedMessage = cipher.encrypt(Encoding.UTF8.GetBytes(message.Body));
            byte[] encryptedData = encryptedMessage.serialize();

            message.Body = "You received an encrypted message, but your client doesn't seem to support OMEMO.";

            message.Data.Child(Xml.Element("encryption", "urn:xmpp:eme:0").Attr("name", "OMEMO").Attr("namespace", "eu.siacs.conversations.axolotl"));

            XmlElement encryptedElement = Xml.Element("encrypted", "eu.siacs.conversations.axolotl");
            XmlElement headerElement = Xml.Element("header").Attr("sid", sid.ToString());
            XmlElement keyElement = Xml.Element("key").Attr("rid", bundle.DeviceID.ToString()).InnerText(Convert.ToBase64String(pkb.getSignedPreKey().serialize()));
            XmlElement ivElement = Xml.Element("iv").InnerText(Convert.ToBase64String(pkb.getSignedPreKeySignature()));

            headerElement.Child(keyElement);
            headerElement.Child(ivElement);

            encryptedElement.Child(headerElement);

            message.Data.Child(encryptedElement);

            string xml = message.ToString();
        }

        public void Output(Iq stanza)
        {
            if (stanza.Data?.SelectSingleNode("//publish")?.Attributes["node"]?.Value == "eu.siacs.conversations.axolotl.devicelist")
                stanza.OpenAccessModel();

            if (stanza.Data?.SelectSingleNode("//publish")?.Attributes["node"]?.Value.StartsWith("eu.siacs.conversations.axolotl.bundles:") ?? false)
                stanza.OpenAccessModel();
        }

    }
}
