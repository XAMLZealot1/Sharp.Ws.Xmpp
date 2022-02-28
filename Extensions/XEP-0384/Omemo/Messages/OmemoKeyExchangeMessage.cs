using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Xmpp.Omemo.Exceptions;
using System;

namespace Sharp.Ws.Xmpp.Extensions.Omemo.Messages
{
    /// <summary>
    /// Message based on: https://xmpp.org/extensions/xep-0384.html#protobuf-schema
    /// </summary>
    public class OmemoKeyExchangeMessage : IOmemoMessage
    {

        public OmemoKeyExchangeMessage(byte[] data)
        {
            ParseBytes(data);
        }

        public OmemoKeyExchangeMessage(uint pkId, uint spkId, ECPubKey ik, ECPubKey ek, OmemoAuthenticatedMessage message)
        {
            PKID = pkId;
            SPKID = spkId;
            IK = ik;
            EK = ek;
            Message = message;
        }

        public ECPubKey EK { get; private set; }

        public ECPubKey IK { get; private set; }

        public OmemoAuthenticatedMessage Message { get; private set; }

        public uint PKID { get; private set; }

        public uint SPKID { get; private set; }

        public byte[] ToByteArray()
        {
            byte[] msg = Message.ToByteArray();
            byte[] result = new byte[4 + 4 + IK.Key.Length + EK.Key.Length + msg.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(PKID), 0, result, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(SPKID), 0, result, 4, 4);
            Buffer.BlockCopy(IK.Key, 0, result, 8, IK.Key.Length);
            Buffer.BlockCopy(EK.Key, 0, result, 8 + IK.Key.Length, EK.Key.Length);
            Buffer.BlockCopy(msg, 0, result, 8 + IK.Key.Length + EK.Key.Length, msg.Length);
            return result;
        }

        public void Validate()
        {
            if (IK?.Key is null || IK.Key.Length != KeyHelper.PUB_KEY_SIZE)
            {
                throw new OmemoException("Invalid " + nameof(OmemoKeyExchangeMessage) + " IK.Key.Length: " + IK?.Key?.Length);
            }
            if (EK?.Key is null || EK.Key.Length != KeyHelper.PUB_KEY_SIZE)
            {
                throw new OmemoException("Invalid " + nameof(OmemoKeyExchangeMessage) + " IK.Key.Length: " + EK?.Key?.Length);
            }
            if (Message is null)
            {
                throw new OmemoException("Invalid " + nameof(OmemoKeyExchangeMessage) + " MESSAGE is null.");
            }
            Message.Validate();
        }

        private void ParseBytes(byte[] data)
        {
            PKID = BitConverter.ToUInt32(data, 0);
            SPKID = BitConverter.ToUInt32(data, 4);
            IK = new ECPubKey(data);
            Buffer.BlockCopy(data, 8, IK.Key, 0, IK.Key.Length);
            EK = new ECPubKey(new byte[KeyHelper.PUB_KEY_SIZE]);
            Buffer.BlockCopy(data, 8 + IK.Key.Length, EK.Key, 0, EK.Key.Length);
            byte[] msg = new byte[data.Length - 4 - 4 - IK.Key.Length - EK.Key.Length];
            Buffer.BlockCopy(data, 8 + IK.Key.Length + EK.Key.Length, msg, 0, msg.Length);
            Message = new OmemoAuthenticatedMessage(msg);
        }

        public override bool Equals(object obj)
        {
            return obj is OmemoKeyExchangeMessage msg && msg.PKID == PKID && msg.SPKID == SPKID && msg.IK.Equals(IK) && msg.EK.Equals(EK) && msg.Message.Equals(Message);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}