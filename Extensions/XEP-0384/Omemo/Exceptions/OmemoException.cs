using System;

namespace Sharp.Xmpp.Omemo.Exceptions
{
    public class OmemoException : Exception
    {
        public OmemoException(string message) : base(message) { }
        public OmemoException(string message, Exception innerException) : base(message, innerException) { }
    }
}
