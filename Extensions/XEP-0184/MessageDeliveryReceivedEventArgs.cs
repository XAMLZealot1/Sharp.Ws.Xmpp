using System;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the MessageDeliveryReceived event.
    /// </summary>
    public class MessageDeliveryReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The Id of the message which has been delivered
        /// </summary>
        public string MessageId
        {
            get;
            private set;
        }

        /// <summary>
        /// The Id of the message which has been delivered
        /// </summary>
        public ReceiptType ReceiptType
        {
            get;
            private set;
        }

        /// <summary>
        /// The timestamp of the delivery
        /// </summary>
        public DateTime Timestamp
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the VCardChangedEventArgs class.
        /// </summary>
        /// <exception cref="ArgumentNullException">The jid parameter is null.</exception>
        public MessageDeliveryReceivedEventArgs(string messageId, ReceiptType receiptType, DateTime timestamp)
        {
            MessageId = messageId;
            ReceiptType = receiptType;
            Timestamp = timestamp;
        }
    }
}
