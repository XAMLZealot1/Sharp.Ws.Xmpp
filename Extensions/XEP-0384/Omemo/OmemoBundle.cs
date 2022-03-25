using libsignal;
using libsignal.ecc;
using libsignal.state;
using Sharp.Ws.Xmpp.Extensions.Interfaces;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Xmpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class OmemoBundle
    {
        internal static readonly string BundleNodePattern = "eu.siacs.conversations.axolotl.bundles:(\\d+)";
        private static readonly uint RemoteRegistrationID = uint.MaxValue;
        private org.whispersystems.curve25519.Curve25519 curve = org.whispersystems.curve25519.Curve25519.getInstance(org.whispersystems.curve25519.Curve25519.BEST);

        public OmemoBundle(Jid jid)
        {
            JabberID = jid;
        }
        public OmemoBundle(XmlElement e, Jid jid) : this(jid)
        {
            ParseDeviceID(e.GetAttribute("node"));
            ParseElement(e["item"]);
        }
        public OmemoBundle (XmlElement bundleItem, string node)
        {
            ParseDeviceID(node);
            ParseElement(bundleItem);
        }
        public OmemoBundle(PreKeyBundle bundle, IEnumerable<PreKeyRecord> preKeys)
        {
            GenerateBundle(bundle, preKeys);
        }
        public OmemoBundle(PreKeyBundle bundle, IEnumerable<OmemoKey> keys)
        {
            SignedPreKeyPublic = new OmemoKey(Convert.ToBase64String(bundle.getSignedPreKey().serialize()), bundle.getSignedPreKeyId());
            SignedPreKeySignature = bundle.getSignedPreKeySignature();
            IdentityKeyData = bundle.getIdentityKey().serialize();
            DeviceID = bundle.getDeviceId();
            PreKeys.AddRange(keys);
        }

        public uint DeviceID { get; set; }

        public byte[] IdentityKeyData { get; set; }

        public Jid JabberID { get; set; }

        public List<OmemoKey> PreKeys { get; set; } = new List<OmemoKey>();

        public OmemoKey SignedPreKeyPublic { get; set; }

        public byte[] SignedPreKeySignature { get; set; }

        private void GenerateBundle(PreKeyBundle bundle, IEnumerable<PreKeyRecord> preKeys)
        {
            RootElement = Xml.Element("pubsub", "http://jabber.org/protocol/pubsub");
            PublishElement = Xml.Element("publish").Attr("node", $"eu.siacs.conversations.axolotl.bundles:{bundle.getDeviceId()}");
            ItemElement = Xml.Element("item").Attr("id", "current");
            BundleElement = Xml.Element("bundle", "eu.siacs.conversations.axolotl");

            SPKPublicElement = Xml.Element("signedPreKeyPublic").Attr("signedPreKeyId", bundle.getSignedPreKeyId().ToString()).InnerText(Convert.ToBase64String(bundle.getSignedPreKey().serialize()));
            SPKSignatureElement = Xml.Element("signedPreKeySignature").InnerText(Convert.ToBase64String(bundle.getSignedPreKeySignature()));
            IdentityKeyElement = Xml.Element("identityKey").InnerText(Convert.ToBase64String(bundle.getIdentityKey().getPublicKey().serialize()));
            PreKeysElement = Xml.Element("prekeys");

            foreach (var preKey in preKeys)
            {
                XmlElement pk = Xml.Element("preKeyPublic").Attr("preKeyId", preKey.getId().ToString()).InnerText(Convert.ToBase64String(preKey.serialize()));
                PreKeysElement.Child(pk);
            }

            PreKeys = preKeys.Select(x => new OmemoKey(x)).ToList();

            BundleElement.Child(SPKPublicElement);
            BundleElement.Child(SPKSignatureElement);
            BundleElement.Child(IdentityKeyElement);
            BundleElement.Child(PreKeysElement);

            ItemElement.Child(BundleElement);
            PublishElement.Child(ItemElement);
            RootElement.Child(PublishElement);

        }

        private void ParseElement(XmlElement bundleItem)
        {
            var bundleNode = bundleItem["bundle"];

            if (bundleNode == null)
                return;

            SPKPublicElement = bundleNode["signedPreKeyPublic"];
            SPKSignatureElement = bundleNode["signedPreKeySignature"];
            IdentityKeyElement = bundleNode["identityKey"];
            PreKeysElement = bundleNode["prekeys"];

            SignedPreKeyPublic = new OmemoKey(SPKPublicElement);
            SignedPreKeySignature = Convert.FromBase64String(SPKSignatureElement?.InnerText);
            IdentityKeyData = Convert.FromBase64String(IdentityKeyElement?.InnerText);

            if (PreKeysElement == null)
                return;

            foreach (var preKey in PreKeysElement.ChildNodes)
                PreKeys.Add(new OmemoKey((XmlElement)preKey));
        }

        private void ParseDeviceID(string node)
        {
            var match = Regex.Match(node, BundleNodePattern);
            if (match == null || !match.Success)
                return;

            string deviceID = match.Groups[1].Value;
            if (uint.TryParse(deviceID, out uint did))
                DeviceID = did;
        }

        internal PreKeyBundle ToPreKey()
        {
            OmemoKey preKey = PreKeys.SelectRandomItem<OmemoKey>();

            return new PreKeyBundle
            (
                RemoteRegistrationID,
                DeviceID,
                preKey.PreKeyID,
                Curve.decodePoint(preKey.KeyData, 0),
                SignedPreKeyPublic.PreKeyID,
                Curve.decodePoint(SignedPreKeyPublic.KeyData, 0),
                SignedPreKeySignature,
                new IdentityKey(IdentityKeyData, 0)
            );
        }

        #region Xml

        public XmlElement BundleElement { get; set; }

        public XmlElement IdentityKeyElement { get; set; }

        public XmlElement ItemElement { get; set; }

        public XmlElement PreKeysElement { get; set; }

        public XmlElement PublishElement { get; set; }

        public XmlElement PubSubElement { get; set; }

        public XmlElement RootElement { get; private set; }

        public XmlElement SPKPublicElement { get; set; }

        public XmlElement SPKSignatureElement { get; set; }

        #endregion

    }
}
