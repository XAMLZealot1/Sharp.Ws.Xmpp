using libsignal.state;
using Sharp.Ws.Xmpp.Extensions.Omemo.Storage;
using Sharp.Xmpp;
using Sharp.Xmpp.Client;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class OmemoManager
    {
        private readonly OmemoEncryption omemoEncryption;

        internal OmemoManager(OmemoEncryption extension, IRegistrationStore registrar, SignalProtocolStore store)
        {
            extension.InitializeRegistration(registrar);
            extension.InitializeSignal(store);
            omemoEncryption = extension;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jid">The user for which to request device list. If null, will request own devices.</param>
        public IEnumerable<OmemoDevice> GetDeviceList(Jid jid = null)
        {
            return omemoEncryption.GetDeviceList(jid);
        }

        public void StartSession(Jid jid)
        {
            
        }

    }
}
