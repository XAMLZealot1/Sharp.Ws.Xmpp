using Sharp.Xmpp;
using Sharp.Xmpp.Core;
using Sharp.Xmpp.Extensions;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Xml;
using Message = Sharp.Xmpp.Im.Message;

namespace Sharp.Ws.Xmpp.Extensions
{
    internal class StanzaContentEncryption : XmppExtension
    {
        internal const string NS_EXTENSION = "urn:xmpp:sce:1";

        public StanzaContentEncryption(XmppIm im) : base (im)
        { }

        /// <summary>
        /// A reference to the 'Entity Capabilities' extension instance.
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
                return new string[] { NS_EXTENSION };
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
                return Extension.StanzaContentEncryption;
            }
        }

        /// <summary>
        /// Invoked after all extensions have been loaded.
        /// </summary>
        public override void Initialize()
        {
            ecapa = im.GetExtension<EntityCapabilities>();
        }

        private string CreateEnvelope(Stanza stanza)
        {
            var envelope = stanza.Data.Child(Xml.Element("envelope", "urn:xmpp:sce:1"));

            envelope.Child(Xml.Element("content")).Child(Xml.Element("body", "jabber:client"));
            envelope.Child(Xml.Element("time").Attr("stamp", DateTime.UtcNow.ToString("zzz")));
            envelope.Child(Xml.Element("to").Attr("jid", stanza.To.GetBareJid().ToString()));
            envelope.Child(Xml.Element("from").Attr("jid", stanza.From.GetBareJid().ToString()));

            var rpad = Xml.Element("rpad");
            rpad.InnerText = GenerateRandomPadding();

            envelope.Child(rpad);

            return "";
        }

        private string GenerateRandomPadding()
        {
            Random res = new Random();
            int length = res.Next(0, 200);
            String str = "abcdefghijklmnopqrstuvwxyz0123456789`~@#$%^&*()_-+=[]{}|':;?/.>,<";

            // Initializing the empty string
            String randomstring = "";

            for (int i = 0; i < length; i++)
            {

                // Selecting a index randomly
                int x = res.Next(str.Length);

                // Appending the character at the 
                // index to the random alphanumeric string.
                randomstring = randomstring + str[x];
            }

            return randomstring;
        }

        internal void SendMessage(Message message, byte[] encryptedData)
        {

        }

        public bool Input(Stanza stanza)
        {
            return false;
        }
    }
}
