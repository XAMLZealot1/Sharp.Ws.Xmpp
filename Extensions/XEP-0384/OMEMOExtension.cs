using XMPP.Net.Extensions;
using XMPP.Net.Im;
using System;
using System.Collections.Generic;
using System.Text;
using OMEMO.Net.Axolotl;

namespace Sharp.Ws.Xmpp.Extensions
{
    internal class OMEMOExtension : XmppExtension
    {
        private EntityCapabilities ecapa;

        public static string PEP_PREFIX = "eu.siacs.conversations.axolotl";
        public static string PEP_DEVICE_LIST = PEP_PREFIX + ".devicelist";
        public static string PEP_DEVICE_LIST_NOTIFY = PEP_DEVICE_LIST + "+notify";
        public static string PEP_BUNDLES = PEP_PREFIX + ".bundles";
        public static string PEP_VERIFICATION = PEP_PREFIX + ".verification";
        public static string PEP_OMEMO_WHITELISTED = PEP_PREFIX + ".whitelisted";

        public AxolotlStore Store { get; private set; }

        public OMEMOExtension(XmppIm im) : base(im) { }

        public override IEnumerable<string> Namespaces => new string[] { PEP_PREFIX };

        public override Extension Xep => Extension.OMEMO;

        public override void Initialize()
        {
            ecapa = im.GetExtension<EntityCapabilities>();
        }
    }
}
