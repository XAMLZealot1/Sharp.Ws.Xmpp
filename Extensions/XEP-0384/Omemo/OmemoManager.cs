using libsignal;
using libsignal.protocol;
using libsignal.state;
using libsignal.util;
using Sharp.Ws.Xmpp.Extensions.Interfaces;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Xmpp;
using Sharp.Xmpp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class OmemoManager
    {
        private readonly OmemoEncryption omemoEncryption;
        private readonly Jid myJid;
        private readonly SignalProtocolStore myStore;
        
        public SignalProtocolAddress Address { get; private set; }

        public OmemoBundle MyBundle { get; private set; }

        public OmemoDevice MyDevice { get; private set; }

        internal OmemoManager(OmemoEncryption extension, SignalProtocolStore myStore, Jid jid, uint deviceID, IPreKeyCollection preKeyStore)
        {
            myJid = jid;
            this.myStore = myStore;
            omemoEncryption = extension;
            LoadDevice(jid, deviceID);
            LoadBundle(jid, deviceID, myStore, preKeyStore);
            Address = new SignalProtocolAddress(jid.GetBareJid().ToString(), MyDevice.DeviceID);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jid">The user for which to request device list. If null, will request own devices.</param>
        public IEnumerable<OmemoDevice> GetDeviceList(Jid jid)
        {
            return omemoEncryption.GetDeviceList(jid);
        }

        private void LoadBundle(Jid jid, uint deviceID, SignalProtocolStore myStore, IPreKeyCollection preKeyStore)
        {
            //MyBundle = omemoEncryption.GetBundle(jid, deviceID);

            //if (MyBundle != null)
            //    return;

            MyBundle = omemoEncryption.PublishBundle(myStore, deviceID, preKeyStore);
        }

        private void LoadDevice(Jid jid, uint deviceID)
        {
            MyDevice = GetDeviceList(jid).FirstOrDefault(x => x.DeviceID == deviceID);

            if (MyDevice != null)
                return;
                
            MyDevice = omemoEncryption.PublishDeviceList(deviceID, jid);
        }

        public OmemoMessage ReceiveMessage(Sharp.Xmpp.Im.Message message)
        {
            var result = new OmemoMessage(message.Data);

            result.Decrypt(MyDevice.DeviceID, myStore);

            return result;
        }

        public void SendMessage(OmemoBundle bundle, Sharp.Xmpp.Im.Message message)
        {
            omemoEncryption.SendMessage(myStore, bundle, message, MyDevice.DeviceID);
        }


    }
}
