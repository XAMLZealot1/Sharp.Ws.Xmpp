using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Storage
{
    public class InMemoryOmemoStorage : IOmemoStorage
    {
        public readonly Dictionary<OmemoProtocolAddress, OmemoSession> SESSIONS = new Dictionary<OmemoProtocolAddress, OmemoSession>();
        public readonly Dictionary<OmemoProtocolAddress, OmemoFingerprint> FINGERPRINTS = new Dictionary<OmemoProtocolAddress, OmemoFingerprint>();
        public readonly Dictionary<string, List<Tuple<OmemoProtocolAddress, string>>> DEVICES = new Dictionary<string, List<Tuple<OmemoProtocolAddress, string>>>();


        public Tuple<OmemoDeviceListSubscriptionState, DateTime> LoadDeviceListSubscription(string bareJid)
        {
            throw new NotImplementedException();
        }

        public List<Tuple<OmemoProtocolAddress, string>> LoadDevices(string bareJid)
        {
            return DEVICES.ContainsKey(bareJid) ? DEVICES[bareJid] : new List<Tuple<OmemoProtocolAddress, string>>();
        }

        public OmemoFingerprint LoadFingerprint(OmemoProtocolAddress address)
        {
            return FINGERPRINTS.ContainsKey(address) ? FINGERPRINTS[address] : null;
        }

        public OmemoSession LoadSession(OmemoProtocolAddress address)
        {
            return SESSIONS.ContainsKey(address) ? SESSIONS[address] : null;
        }

        public PreKey ReplaceOmemoPreKey(PreKey preKey)
        {
            return preKey;
        }

        public void StoreDeviceListSubscription(string bareJid, Tuple<OmemoDeviceListSubscriptionState, DateTime> lastUpdate)
        {
            throw new NotImplementedException();
        }

        public void StoreDevices(List<Tuple<OmemoProtocolAddress, string>> devices, string bareJid)
        {
            DEVICES[bareJid] = devices;
        }

        public void StoreFingerprint(OmemoFingerprint fingerprint)
        {
            FINGERPRINTS[fingerprint.ADDRESS] = fingerprint;
        }

        public void StoreSession(OmemoProtocolAddress address, OmemoSession session)
        {
            SESSIONS[address] = session;
        }
    }
}
