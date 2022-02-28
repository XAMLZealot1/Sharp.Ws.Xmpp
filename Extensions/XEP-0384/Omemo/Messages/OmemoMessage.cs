using Sharp.Xmpp.Omemo.Exceptions;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using System;
using System.Linq;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Messages
{
    /// <summary>
    /// Message based on: https://xmpp.org/extensions/xep-0384.html#protobuf-schema
    /// </summary>
    public class OmemoMessage : IOmemoMessage
    {
        /// <summary>
        /// Message number.
        /// </summary>
        public readonly uint N;
        /// <summary>
        /// Number of messages in the previous sending chain.
        /// </summary>
        public readonly uint PN;
        /// <summary>
        /// The sender public key part of the encryption key.
        /// </summary>
        public readonly ECPubKey DH;
        public byte[] cipherText;

        /// <summary>
        /// The minimum size in bytes for a valid version of this message.
        /// </summary>
        public static int MIN_SIZE = sizeof(uint) + sizeof(uint) + KeyHelper.PUB_KEY_SIZE;

        public OmemoMessage(byte[] data)
        {
            N = (uint)BitConverter.ToInt32(data, 0);
            PN = (uint)BitConverter.ToInt32(data, 4);
            DH = new ECPubKey(new byte[KeyHelper.PUB_KEY_SIZE]);
            Buffer.BlockCopy(data, 8, DH.Key, 0, DH.Key.Length);
            int cipherTextLenth = data.Length - (8 + DH.Key.Length);

            // Cipher text here is optional:
            if (cipherTextLenth > 0)
            {
                cipherText = new byte[data.Length - (8 + DH.Key.Length)];
                Buffer.BlockCopy(data, 8 + DH.Key.Length, cipherText, 0, cipherText.Length);
            }
            Validate();
        }

        public OmemoMessage(OmemoSession session)
        {
            N = session.nS;
            PN = session.pn;
            DH = session.dhS.pubKey;
            cipherText = null;
        }

        public byte[] ToByteArray()
        {
            int size = 4 + 4 + DH.Key.Length;
            if (!(cipherText is null))
            {
                size += cipherText.Length;
            }
            byte[] result = new byte[size];
            Buffer.BlockCopy(BitConverter.GetBytes(N), 0, result, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(PN), 0, result, 4, 4);
            Buffer.BlockCopy(DH.Key, 0, result, 8, DH.Key.Length);
            if (!(cipherText is null))
            {
                Buffer.BlockCopy(cipherText, 0, result, 8 + DH.Key.Length, cipherText.Length);
            }
            return result;
        }

        public void Validate()
        {
            if (DH?.Key is null || DH.Key.Length != KeyHelper.PUB_KEY_SIZE)
            {
                throw new OmemoException("Invalid " + nameof(OmemoMessage) + " DH.key.Length: " + DH?.Key?.Length);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is OmemoMessage msg && msg.N == N && msg.PN == PN && msg.DH.Equals(DH) && ((msg.cipherText is null && cipherText is null) || msg.cipherText.SequenceEqual(cipherText));
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

    }
}
