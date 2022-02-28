using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using System.Collections.Generic;
using X25519;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public static class KeyHelper
    {
        public static int PUB_KEY_SIZE = 32;
        public static int PRIV_KEY_SIZE = 32;

        /// <summary>
        /// Generates a new Ed25519 <see cref="IdentityKeyPair"/> and returns it.
        /// </summary>
        public static IdentityKeyPair GenerateIdentityKeyPair()
        {
            GenericECKeyPair pair = GenerateKeyPair();
            return new IdentityKeyPair(pair.privKey, pair.pubKey);
        }

        /// <summary>
        /// Generates a list of <see cref="PreKey"/>s and returns them.
        /// <para/>
        /// To keep the <see cref="PreKey"/>-IDs unique ensure to set start to (start + count) of the last run.
        /// </summary>
        /// <param name="start">The start it of the new <see cref="PreKey"/>.</param>
        /// <param name="count">How many <see cref="PreKey"/>s should be generated.</param>
        public static List<PreKey> GeneratePreKeys(uint start, uint count)
        {
            List<PreKey> preKeys = new List<PreKey>();
            for (uint i = start; i < (start + count); i++)
            {
                preKeys.Add(GeneratePreKey(i));
            }
            return preKeys;
        }

        /// <summary>
        /// Generates a new Ed25519 <see cref="PreKey"/> and returns it.
        /// </summary>
        /// <param name="id">The id of the <see cref="PreKey"/>.</param>
        public static PreKey GeneratePreKey(uint id)
        {
            GenericECKeyPair pair = GenerateKeyPair();
            return new PreKey(pair.privKey, pair.pubKey, id);
        }

        /// <summary>
        /// Generates a new Ed25519 <see cref="SignedPreKey"/> and returns it.
        /// </summary>
        /// <param name="id">The id of the <see cref="SignedPreKey"/>.</param>
        /// <param name="identiyKey">The private part of an <see cref="IdentityKeyPair"/> used for signing.</param>
        public static SignedPreKey GenerateSignedPreKey(uint id, ECPrivKey identiyKey)
        {
            PreKey preKey = GeneratePreKey(id);
            
            byte[] signature = SignatureAlgorithm.Ed25519.Sign(Key.Import(SignatureAlgorithm.Ed25519, identiyKey.Key, KeyBlobFormat.RawPrivateKey), preKey.pubKey.Key);
            return new SignedPreKey(preKey, signature);
        }

        /// <summary>
        /// Verifies the signature of the given data with the given <see cref="ECPubKey"/>.
        /// </summary>
        /// <param name="pubKey">The <see cref="ECPubKey"/> used for validating the signature.</param>
        /// <param name="data">The data the signature should be verified for.</param>
        /// <param name="signature">The signature that should be verified.</param>
        /// <returns></returns>
        public static bool VerifySignature(ECPubKey pubKey, byte[] data, byte[] signature)
        {
            return SignatureAlgorithm.Ed25519.Verify(PublicKey.Import(SignatureAlgorithm.Ed25519, pubKey.Key, KeyBlobFormat.RawPublicKey), data, signature);
        }

        /// <summary>
        /// Generates a new Ed25519 <see cref="EphemeralKeyPair"/> and returns it.
        /// </summary>
        public static EphemeralKeyPair GenerateEphemeralKeyPair()
        {
            GenericECKeyPair pair = GenerateKeyPair();
            return new EphemeralKeyPair(pair.privKey, pair.pubKey);
        }

        /// <summary>
        /// Generates a new Ed25519 <see cref="GenericECKeyPair"/> and returns it.
        /// </summary>
        public static GenericECKeyPair GenerateKeyPair()
        {
            X25519KeyPair pair = X25519KeyAgreement.GenerateKeyPair();
            return new GenericECKeyPair(new ECPrivKey(pair.PrivateKey), new ECPubKey(pair.PublicKey));
        }

        /// <summary>
        /// Generates a 32 byte long cryptographically secure random data symmetric key and returns it.
        /// </summary>
        public static byte[] GenerateSymetricKey()
        {
            return CryptoUtils.NextBytesSecureRandom(32);
        }

    }
}
