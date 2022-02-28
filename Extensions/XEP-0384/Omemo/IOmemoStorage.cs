using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using System;
using System.Collections.Generic;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public interface IOmemoStorage
    {
        /// <summary>
        /// Checks and returns the <see cref="OmemoSession"/> for the given <see cref="OmemoProtocolAddress"/> or null in case no session for this <see cref="OmemoProtocolAddress"/> exists.
        /// </summary>
        /// <param name="address">The <see cref="OmemoProtocolAddress"/> a session should be retrieved.</param>
        OmemoSession LoadSession(OmemoProtocolAddress address);
        /// <summary>
        /// Stores the session for the given <see cref="OmemoProtocolAddress"/>.
        /// </summary>
        /// <param name="address">The address for the session.</param>
        /// <param name="session">The session to store.</param>
        void StoreSession(OmemoProtocolAddress address, OmemoSession session);

        void StoreDeviceListSubscription(string bareJid, Tuple<OmemoDeviceListSubscriptionState, DateTime> lastUpdate);

        /// <summary>
        /// Loads and returns the device list subscription state for the given bare JID and returns it.
        /// </summary>
        /// <param name="bareJid">The bare JID you want to retrieve the device list subscription state for.</param>
        Tuple<OmemoDeviceListSubscriptionState, DateTime> LoadDeviceListSubscription(string bareJid);

        /// <summary>
        /// Loads all OMEMO devices for the given bare JID and returns them.
        /// In case no devices were found an empty list will be returned.
        /// Each device consists of its <see cref="OmemoProtocolAddress"/> and an optional label.
        /// </summary>
        /// <param name="bareJid">The bare JID you want to retrieve all devices for.</param>
        List<Tuple<OmemoProtocolAddress, string>> LoadDevices(string bareJid);

        /// <summary>
        /// Stores the given OMEMO device.
        /// </summary>
        /// <param name="devices">The devices to store. A tuple with the <see cref="OmemoProtocolAddress"/> and an optional label for this device.</param>
        /// <param name="bareJid">The bare JID you want to store the device list for.</param>
        void StoreDevices(List<Tuple<OmemoProtocolAddress, string>> devices, string bareJid);

        /// <summary>
        /// Loads and returns the OMEMO fingerprint to the given <see cref="OmemoProtocolAddress"/>.
        /// </summary>
        /// <param name="address">The <see cref="OmemoProtocolAddress"/> you want to retrieve the <see cref="OmemoFingerprint"/> for.</param>
        OmemoFingerprint LoadFingerprint(OmemoProtocolAddress address);

        /// <summary>
        /// Stores the given <see cref="OmemoFingerprint"/>.
        /// </summary>
        /// <param name="fingerprint">The <see cref="OmemoFingerprint"/> to store.</param>
        void StoreFingerprint(OmemoFingerprint fingerprint);

        /// <summary>
        /// Replaces the given <see cref="PreKey"/> with a new one for the current account and returns it.
        /// </summary>
        /// <param name="preKey">The <see cref="PreKey"/> that should be replaced by a new one.</param>
        /// <returns>A fresh <see cref="PreKey"/>.</returns>
        PreKey ReplaceOmemoPreKey(PreKey preKey);
    }
}
