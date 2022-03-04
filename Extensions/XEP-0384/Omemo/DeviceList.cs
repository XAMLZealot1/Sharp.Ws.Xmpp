using Sharp.Xmpp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class DeviceList
    {

        public DeviceList()
        {

        }
        internal DeviceList(XmlElement e, Jid jid) : this()
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
                Devices.Add(new OmemoDevice((XmlElement)deviceNode));
        }

        public List<OmemoDevice> Devices { get; set; } = new List<OmemoDevice>();

        public Jid Jid { get; set; }

    }

    public class OmemoDevice
    {
        public OmemoDevice() { }
        internal OmemoDevice(XmlElement e) : this()
        {
            if (e == null)
                return;

            if (uint.TryParse(e.GetAttribute("id"), out uint id))
                DeviceID = id;
        }

        public uint DeviceID { get; set; }

    }

}
