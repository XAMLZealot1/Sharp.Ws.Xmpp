using System.Collections.Generic;
using System.Diagnostics;

namespace XMPP.Net.Extensions
{
    internal class FetchStatusMap : AxolotlAddressMap<FetchStatus>
    {
        private object locker = new object();

        public void ClearErrorFor(Jid jid)
        {
            lock (locker)
            {
                Dictionary<int, FetchStatus> devices = map[jid.GetBareJid().ToString()];
                if (devices == null)
                    return;
                foreach (KeyValuePair<int, FetchStatus> entry in devices)
                {
                    if (entry.Value == FetchStatus.ERROR)
                    {
                        Debug.WriteLine($"Resetting error for {jid.GetBareJid()} ({entry.Key})");
                        devices[entry.Key] = FetchStatus.TIMEOUT;
                    }
                }
            };
        }

    }

    public enum FetchStatus
    {
        PENDING,
        SUCCESS,
        SUCCESS_VERIFIED,
        TIMEOUT,
        SUCCESS_TRUSTED,
        ERROR
    }

}
