using lib.Utils;

namespace lib.net.Packet
{
    public interface IPacket
    {
        public EPacketType PacketType { get; }
        public string Key { get; }
        public byte[] Value { get; }

        public Span<byte> Serialize();
        public static abstract IPacket DeserializePacket(Span<byte> data);


        public bool Equals(IPacket? obj)
        {
            if (obj is IPacket)
            {
                IPacket packet = (IPacket)obj;
                bool ret = true;
                ret &= PacketType == packet.PacketType;
                ret &= Key == packet.Key;
                ret &= Value.Eq(packet.Value);
                return ret;
            }
            return false;
        }
    }
}
