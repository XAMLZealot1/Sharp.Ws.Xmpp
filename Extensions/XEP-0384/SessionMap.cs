using libsignal;
using OMEMO.Net.Axolotl;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using XMPP.Net.Im;

namespace XMPP.Net.Extensions
{
    internal class SessionMap : AxolotlAddressMap<XmppAxolotlSession>
    {
        private XmppIm im;

        public SessionMap(XmppIm im, AxolotlStore store) : base()
        {
            this.im = im;
        }

        private void FillMap(AxolotlStore store)
        {
            List<uint> deviceIds = store.GetSubDeviceSessions(im.Jid.GetBareJid().ToString());
            PutDevicesForJid(im.Jid.GetBareJid().ToString(), deviceIds, store);
            foreach (string address in store.GetKnownAddresses())
            {
                deviceIds = store.GetSubDeviceSessions(address);
                PutDevicesForJid(address, deviceIds, store);
            }
        }

        private void PutDevicesForJid(string bareJid, IEnumerable<uint> deviceIds, AxolotlStore store)
        {
            foreach (uint deviceId in deviceIds)
            {
                SignalProtocolAddress axolotlAddress = new SignalProtocolAddress(bareJid, deviceId);
                IdentityKey identityKey = store.LoadSession(axolotlAddress).getSessionState().getRemoteIdentityKey();
                if (OMEMO.Net.Config.X509_VERIFICATION)
                {
                    X509Certificate certificate = store.GetFingerprintCertificate(Convert.ToHexString(identityKey.getPublicKey().serialize()));
                    if (certificate != null)
                    {
                        // TODO: Implement certificate parsing
                        throw new NotImplementedException();
                    }
                }
                Put(axolotlAddress, new XmppAxolotlSession(new Client.XmppAccount(bareJid), store, axolotlAddress, identityKey));
            }
        }

        public override void Put(SignalProtocolAddress address, XmppAxolotlSession value)
        {
            base.Put(address, value);
            value.SetNotFresh();
        }

        public void Put(XmppAxolotlSession session)
        {
            Put(session.RemoteAddress, session);
        }

    }
}
