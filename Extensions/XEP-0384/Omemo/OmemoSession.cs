using Newtonsoft.Json;
using Sharp.Ws.Xmpp.Extensions.Omemo.Keys;
using Sharp.Ws.Xmpp.Extensions.Omemo.Messages;
using System;

namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    /// <summary>
    /// The current session state.
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// We have send a message, but have not received one yet.
        /// </summary>
        SEND,
        /// <summary>
        /// We have received a message, but have not send one yet.
        /// </summary>
        RECEIVED,
        /// <summary>
        /// We send and received a message and the session is build.
        /// </summary>
        READY
    }


    public class OmemoSession
    {
        /// <summary>
        /// DB key for storing a session in a DB.
        /// </summary>
        public int id
        {
            get => _id;
            set => _id = value;
        }
        private int _id;

        /// <summary>
        /// The current state of the session.
        /// </summary>
        public SessionState state
        {
            get => _state;
            set => _state = value;
        }
        private SessionState _state;

        /// <summary>
        /// Key pair for the sending ratchet.
        /// </summary>
        public GenericECKeyPair dhS
        {
            get => _dhS;
            set => _dhS = value;
        }
        private GenericECKeyPair _dhS;

        /// <summary>
        /// Key pair for the receiving ratchet.
        /// </summary>
        public ECPubKey dhR
        {
            get => _dhR;
            set => _dhR = value;
        }
        private ECPubKey _dhR;

        /// <summary>
        /// Ephemeral key used for initiating this session. 
        /// </summary>
        public ECPubKey ek
        {
            get => _ek;
            set => _ek = value;
        }
        private ECPubKey _ek;

        /// <summary>
        /// 32 byte root key for encryption.
        /// </summary>
        public byte[] rk
        {
            get => _rk;
            set => _rk = value;
        }
        private byte[] _rk;

        /// <summary>
        /// 32 byte Chain Keys for sending.
        /// </summary>
        public byte[] ckS
        {
            get => _ckS;
            set => _ckS = value;
        }
        private byte[] _ckS;

        /// <summary>
        /// 32 byte Chain Keys for receiving.
        /// </summary>
        public byte[] ckR
        {
            get => _ckR;
            set => _ckR = value;
        }
        private byte[] _ckR;

        /// <summary>
        /// Message numbers for sending.
        /// </summary>
        public uint nS
        {
            get => _nS;
            set => _nS = value;
        }
        private uint _nS;

        /// <summary>
        /// Message numbers for receiving.
        /// </summary>
        public uint nR
        {
            get => _nR;
            set => _nR = value;
        }
        private uint _nR;

        /// <summary>
        /// Number of messages in previous sending chain.
        /// </summary>
        public uint pn
        {
            get => _pn;
            set => _pn = value;
        }
        private uint _pn;

        /// <summary>
        /// Skipped-over message keys, indexed by ratchet <see cref="ECPubKey"/> and message number. Raises an exception if too many elements are stored.
        /// </summary>
        public readonly SkippedMessageKeyGroups MK_SKIPPED = new SkippedMessageKeyGroups();

        /// <summary>
        /// The id of the PreKey used to create establish this session.
        /// </summary>
        public uint preKeyId
        {
            get => _preKeyId;
            set => _preKeyId = value;
        }

        private uint _preKeyId;

        /// <summary>
        /// The id of the signed PreKey used to create establish this session.
        /// </summary>
        public uint signedPreKeyId
        {
            get => _signedPreKeyId;
            set => _preKeyId = value;
        }

        private uint _signedPreKeyId;

        /// <summary>
        /// The associated data is created by concatenating the IdentityKeys of Alice and Bob.
        /// <para/>
        /// AD = Encode(IK_A) || Encode(IK_B).
        /// <para/>
        /// Alice is the party that actively initiated the key exchange, while Bob is the party that passively accepted the key exchange.
        /// </summary>
        public byte[] assData
        {
            get => _assData;
            set => _assData = value;
        }

        private byte[] _assData;

        /// <summary>
        /// Max number of skipped message keys to prevent DOS attacks.
        /// </summary>
        public const uint MAX_SKIP = 100;

        /// <summary>
        /// Creates a new <see cref="OmemoSession"/> for sending a new message and initiating a new key exchange.
        /// </summary>
        /// <param name="receiverBundle">The <see cref="Bundle"/> of the receiving end.</param>
        /// /// <param name="receiverPreKeyIndex">The index of the <see cref="Bundle.preKeys"/> to use.</param>
        /// <param name="senderIdentityKeyPair">Our own <see cref="IdentityKeyPair"/>.</param>
        public OmemoSession(Bundle receiverBundle, int receiverPreKeyIndex, IdentityKeyPair senderIdentityKeyPair)
        {
            EphemeralKeyPair ephemeralKeyPair = KeyHelper.GenerateEphemeralKeyPair();
            byte[] sk = CryptoUtils.GenerateSenderSessionKey(senderIdentityKeyPair.privKey, ephemeralKeyPair.privKey, receiverBundle.identityKey, receiverBundle.signedPreKey, receiverBundle.preKeys[receiverPreKeyIndex].pubKey);

            // We are only interested in the public key and discard the private key.
            ek = ephemeralKeyPair.pubKey;
            dhS = KeyHelper.GenerateKeyPair();
            dhR = receiverBundle.identityKey.Clone();
            (rk, ckS) = LibSignalUtils.KDF_RK(sk, CryptoUtils.SharedSecret(dhS.privKey, dhR));
            signedPreKeyId = receiverBundle.signedPreKeyId;
            preKeyId = receiverBundle.preKeys[receiverPreKeyIndex].keyId;
            assData = CryptoUtils.Concat(senderIdentityKeyPair.pubKey.Key, receiverBundle.identityKey.Key);
            state = SessionState.SEND;
        }

        /// <summary>
        /// Creates a new <see cref="OmemoSession"/> for receiving a new message and accepting a new key exchange.
        /// </summary>
        /// <param name="receiverIdentityKey">The receivers <see cref="IdentityKeyPair"/>.</param>
        /// <param name="receiverSignedPreKey">The receivers <see cref="SignedPreKey"/>.</param>
        /// <param name="receiverPreKey">The receivers <see cref="PreKey"/> selected by the sender.</param>
        /// <param name="keyExchangeMsg">The received <see cref="OmemoKeyExchangeMessage"/>.</param>
        public OmemoSession(IdentityKeyPair receiverIdentityKey, SignedPreKey receiverSignedPreKey, PreKey receiverPreKey, OmemoKeyExchangeMessage keyExchangeMsg)
        {
            dhS = new GenericECKeyPair(receiverIdentityKey.privKey.Clone(), receiverIdentityKey.pubKey.Clone()); // Prevent cascading deletion when we delete an old session
            rk = CryptoUtils.GenerateReceiverSessionKey(keyExchangeMsg.IK, keyExchangeMsg.EK, receiverIdentityKey.privKey, receiverSignedPreKey.preKey.privKey, receiverPreKey.privKey);
            assData = CryptoUtils.Concat(keyExchangeMsg.IK.Key, receiverIdentityKey.pubKey.Key);
            ek = keyExchangeMsg.EK;
            preKeyId = receiverPreKey.keyId;
            state = SessionState.RECEIVED;
        }

        /// <summary>
        /// Initiates the DH ratchet for decrypting the first message.
        /// </summary>
        /// <param name="msg">The received <see cref="OmemoMessage"/>.</param>
        public void InitDhRatchet(OmemoMessage msg)
        {
            pn = nS;
            nS = 0;
            nR = 0;
            dhR = msg.DH.Clone();
            (rk, ckR) = LibSignalUtils.KDF_RK(rk, CryptoUtils.SharedSecret(dhS.privKey, dhR));
            dhS = KeyHelper.GenerateKeyPair();
            (rk, ckS) = LibSignalUtils.KDF_RK(rk, CryptoUtils.SharedSecret(dhS.privKey, dhR));
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

    }
}
