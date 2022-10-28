using System;
using System.Xml;

namespace XMPP.Net.Extensions
{
    /// <summary>
    /// Provides data for the XmlElementEventArgs event
    /// </summary>
    public class XmlElementEventArgs : EventArgs
    {
        /// <summary>
        /// Message string of the event
        /// </summary>
        public XmlElement XmlElement
        {
            get;
            private set;
        }

        public XmlElementEventArgs(XmlElement XmlElement)
        {
            this.XmlElement = XmlElement;
        }
    }
}
