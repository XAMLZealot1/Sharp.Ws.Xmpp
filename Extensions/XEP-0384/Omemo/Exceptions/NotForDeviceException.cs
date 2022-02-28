using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Xmpp.Omemo.Exceptions
{
    /// <summary>
    /// An exception that gets thrown in case a encrypted message was not encrypted for this device.
    /// </summary>
    public class NotForDeviceException : OmemoException
    {
        public NotForDeviceException(string message) : base(message) { }

    }
}
