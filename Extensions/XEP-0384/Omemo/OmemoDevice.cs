namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    public class OmemoDevice
    {
        public readonly uint DEVICE_ID;
        public readonly OmemoSession SESSION;

        public OmemoDevice(uint deviceId, OmemoSession session)
        {
            DEVICE_ID = deviceId;
            SESSION = session;
        }

    }
}
