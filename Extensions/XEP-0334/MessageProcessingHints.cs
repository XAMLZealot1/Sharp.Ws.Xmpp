using Sharp.Xmpp.Extensions;
using Sharp.Xmpp.Im;
using System.Collections.Generic;

namespace Sharp.Ws.Xmpp.Extensions
{
    internal class MessageProcessingHints : XmppExtension, IInputFilter<Message>
    {
        internal const string NAMESPACE = "urn:xmpp:hints";

        public MessageProcessingHints(XmppIm im) : base(im)
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
                return new string[] { NAMESPACE };
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
                return Extension.MessageProcessingHints;
            }
        }

        /// <summary>
        /// Invoked after all extensions have been loaded.
        /// </summary>
        public override void Initialize()
        {
            ecapa = im.GetExtension<EntityCapabilities>();
        }

        public bool Input(Message stanza)
        {
            return false;
        }
    }
}
