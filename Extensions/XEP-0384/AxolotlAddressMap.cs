using libsignal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace XMPP.Net.Extensions
{
    internal class AxolotlAddressMap<T>
    {
        protected Dictionary<string, Dictionary<int, T>> map;

        protected object MAP_LOCK = new object();

        public AxolotlAddressMap()
        {
            map = new Dictionary<string, Dictionary<int, T>>();
        }

        public virtual void Put(SignalProtocolAddress address, T value)
        {
            lock (MAP_LOCK)
            {
                Dictionary<int, T> devices = map[address.Name];
                if (devices == null)
                {
                    devices = new Dictionary<int, T>();
                    map.Add(address.Name, devices);
                }
                devices.Add(Convert.ToInt32(address.DeviceId), value);
            };
        }

        public T Get(SignalProtocolAddress address)
        {
            lock (MAP_LOCK)
            {
                Dictionary<int, T> devices = map[address.Name];
                if (devices == null)
                    return default(T);

                return devices[Convert.ToInt32(address.DeviceId)];
            };
        }

        public Dictionary<int, T> GetAll(string name)
        {
            lock (MAP_LOCK)
            {
                Dictionary<int, T> devices = map[name];
                if (devices == null)
                    return new Dictionary<int, T>();
                return devices;
            };
        }

        public bool HasAny(SignalProtocolAddress address)
        {
            lock (MAP_LOCK)
            {
                Dictionary<int, T> devices = map[address.Name];
                return devices != null && devices.Any();
            };
        }

        public void Clear()
        {
            map.Clear();
        }
    }
}
