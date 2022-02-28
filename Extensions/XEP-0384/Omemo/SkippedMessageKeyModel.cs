namespace Sharp.Ws.Xmpp.Extensions.Omemo
{
    /// <summary>
    /// Does not derive from <see cref="AbstractDataTemplate"/> since it's just not interesting.
    /// </summary>
    public class SkippedMessageKeyModel
    {

        public int id { get; set; }

        public uint nr { get; set; }

        public byte[] mk { get; set; }

        public SkippedMessageKeyModel() { }
        public SkippedMessageKeyModel(uint nr, byte[] mk)
        {
            this.nr = nr;
            this.mk = mk;
        }

        public override int GetHashCode()
        {
            return (int)nr;
        }

    }
}
