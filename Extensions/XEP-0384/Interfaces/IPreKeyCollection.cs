using System.Collections.Generic;

namespace Sharp.Ws.Xmpp.Extensions.Interfaces
{
    public interface IPreKeyCollection
    {
        IDictionary<uint, byte[]> Store { get; }

        uint GetNextStartID();

        uint GetRandomPreKeyID();
    }
}
