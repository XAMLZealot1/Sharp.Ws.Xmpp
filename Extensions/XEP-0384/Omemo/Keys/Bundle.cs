using System;
using System.Collections.Generic;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Keys
{
    public class Bundle
    {
        /// <summary>
        /// The public part of the signed PreKey.
        /// </summary>
        public ECPubKey signedPreKey;
        /// <summary>
        /// The id of the signed PreKey.
        /// </summary>
        public uint signedPreKeyId;
        /// <summary>
        /// The signature of the signed PreKey.
        /// </summary>
        public byte[] preKeySignature;
        /// <summary>
        /// The public part of the identity key.
        /// </summary>
        public ECPubKey identityKey;
        /// <summary>
        /// A collection of public parts of the <see cref="PreKey"/>s and their ID.
        /// </summary>
        public List<PreKey> preKeys = new List<PreKey>();

        /// <summary>
        /// Returns a secure random index for the <see cref="preKeys"/> list.
        /// </summary>
        public int GetRandomPreKeyIndex()
        {
            return (int)(new Random().Next() % preKeys.Count);
        }

        public PreKey getRandomPreKey()
        {
            Random r = new Random();
            return preKeys[r.Next(0, preKeys.Count)];
        }

        /// <summary>
        /// Verifies the PreKey signature.
        /// </summary>
        /// <returns>True in case the signature is valid.</returns>
        public bool Verify()
        {
            return KeyHelper.VerifySignature(signedPreKey, identityKey.Key, preKeySignature);
        }

    }
}
