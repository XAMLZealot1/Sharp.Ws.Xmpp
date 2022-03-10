using libsignal;
using libsignal.ecc;
using libsignal.state;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Xmpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class DeviceList
    {

        public DeviceList()
        {

        }
        internal DeviceList(XmlElement e, Jid jid, SignalProtocolStore store)
        {
            Jid = jid;

            if (e == null)
                return;

            XmlElement list = e["list"] ?? e["devices"];

            if (list == null)
                return;

            if (!list?.HasChildNodes ?? false)
                return;

            foreach (var deviceNode in list.ChildNodes)
                Devices.Add(new OmemoDevice((XmlElement)deviceNode, jid));
        }

        public List<OmemoDevice> Devices { get; set; } = new List<OmemoDevice>();

        public Jid Jid { get; set; }

    }

    public class OmemoDevice
    {
        public OmemoDevice() { }
        internal OmemoDevice(XmlElement e, Jid jid)
        {
            if (e == null)
                return;

            if (uint.TryParse(e.GetAttribute("id"), out uint id))
                DeviceID = id;

            Jid = jid;
        }
        public OmemoDevice(Jid jid, uint deviceID, OmemoBundle bundle)
        {
            Bundle = bundle;
            DeviceID = deviceID;
            Jid = jid;
        }

        private SignalProtocolAddress address;
        public SignalProtocolAddress Address
        {
            get
            {
                if (address == null)
                    address = new SignalProtocolAddress(Jid?.ToString(), DeviceID);

                return address;
            }
        }

        public OmemoBundle Bundle { get; set; }

        public uint DeviceID { get; set; }

        public Jid Jid { get; set; }

    }

}
