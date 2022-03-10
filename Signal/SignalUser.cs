﻿using libsignal;
using libsignal.ecc;
using libsignal.protocol;
using libsignal.state;
using libsignal.state.impl;
using libsignal.util;
using Sharp.Ws.Xmpp.Extensions.Omemo;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Xmpp;
using Sharp.Xmpp.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sharp.Ws.Xmpp.Signal
{
    public class SignalUser
    {
        private readonly Random random = new Random();

        // Installation Level Information
        protected readonly IdentityKeyPair identityKeyPair;
        protected readonly uint registrationID;
        protected readonly uint deviceID;

        // App Instance Level Information
        protected readonly SignalProtocolStore store;
        private PreKeyBundle preKey;

        // XMPP Account Instance Level Information
        protected readonly XmppClient client;
        internal Jid jid;

        uint preKeyId;

        public SignalProtocolAddress Address { get => new SignalProtocolAddress(jid.GetBareJid().ToString(), deviceID); }

        protected SignalUser(Jid jid, string password)
        {
            this.jid = jid;

            identityKeyPair = KeyHelper.generateIdentityKeyPair();
            registrationID = KeyHelper.generateRegistrationId(false);
            deviceID = Convert.ToUInt32(random.Next());

            store = new InMemorySignalProtocolStore(identityKeyPair, registrationID);

            var preKeys = KeyHelper.generatePreKeys(1000000, 100);
            foreach (var preKey in preKeys)
                store.StorePreKey(preKey.getId(), preKey);

            client = new XmppClient(jid.Domain, jid.Node, password);

        }

        internal PreKeyBundle RequestBundle()
        {
            if (preKey == null)
            {
                ECKeyPair preKeyPair = Curve.generateKeyPair();
                ECKeyPair signedPreKeyPair = Curve.generateKeyPair();
                byte[] signedPreKeySignature = Curve.calculateSignature(store.GetIdentityKeyPair().getPrivateKey(), signedPreKeyPair.getPublicKey().serialize());
                preKeyId = GenerateRandomUint(1000000, 1000100);

                uint signedPreKeyId = GenerateRandomUint();

                preKey = new PreKeyBundle(
                    store.GetLocalRegistrationId(),
                    deviceID, preKeyId, preKeyPair.getPublicKey(), signedPreKeyId,
                    signedPreKeyPair.getPublicKey(), signedPreKeySignature,
                    store.GetIdentityKeyPair().getPublicKey());

                store.StorePreKey(preKeyId, new PreKeyRecord(preKey.getPreKeyId(), preKeyPair));
                store.StoreSignedPreKey(signedPreKeyId, new SignedPreKeyRecord(signedPreKeyId, 0, signedPreKeyPair, signedPreKeySignature));
            }

            return preKey;
        }

        internal OmemoBundle GetBundle()
        {
            var bundle = RequestBundle();
            var prekey = store.LoadPreKey(preKeyId);

            OmemoKey key = new OmemoKey(Convert.ToBase64String(prekey.getKeyPair().getPublicKey().serialize()), preKeyId);

            return new OmemoBundle(bundle, new OmemoKey[] { key });
        }

        internal PreKeyBundle RehydrateBundle(string signedPreKeyPublic, uint signedPreKeyId, string signedPreKeySignature, string identityKey, uint deviceID, uint preKeyID, string preKeyB64)
        {
            ECPublicKey signedPreKey = new DjbECPublicKey(Convert.FromBase64String(signedPreKeyPublic));
            ECPublicKey preKey = new DjbECPublicKey(Convert.FromBase64String(preKeyB64));
            byte[] signature = Convert.FromBase64String(signedPreKeySignature);
            IdentityKey ik = new IdentityKey(Convert.FromBase64String(identityKey), 0);

            var result = new PreKeyBundle
            (
                uint.MaxValue,  // Registration ID
                deviceID,       // Device ID
                preKeyID,       // Pre Key ID
                preKey,         // Pre Key Public
                signedPreKeyId, // Signed Pre Key ID
                signedPreKey,   // Signed Pre Key Public
                signature,      // Signature Bytes
                ik              // Identity Key
            );

            return result;
        }

        protected uint GenerateRandomUint(int lowerBound = 0, int upperBound = int.MaxValue)
        {
            return Convert.ToUInt32(random.Next(lowerBound, upperBound));
        }

        internal void SendMessage(SignalUser sender, string encryptedText)
        {
            PreKeySignalMessage encryptedMessage = new PreKeySignalMessage(Convert.FromBase64String(encryptedText));

            SessionCipher cipher = new SessionCipher(store, sender.Address);

            byte[] plainTextData = cipher.decrypt(encryptedMessage);

            string plainText = Encoding.UTF8.GetString(plainTextData);

            
        }

    }

}
