using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace lib.net.Packet
{
    public readonly struct PathedPacket : IPacket
    {
        public static PathedPacket NOP = new PathedPacket(EPathedPacketType.NOP, new byte[0]);
        SPacket packet { get; init; }

        public EPacketType PacketType { get => packet.PacketType; }
        public string Key { get => packet.Key; }
        public byte[] Value { get => packet.Value; }


        //PathedPacket() { }

        public PathedPacket(EPathedPacketType pt, SPacket packet, params string[] name) : this( pt, packet.Serialize(), name) { }
        public PathedPacket(EPathedPacketType pt, byte[] data,params string[] name) : this(pt, data.AsSpan(), name) { }
        public PathedPacket(EPathedPacketType pt, Span<Byte> data, params string[] name)
        {
            
            List<string> names = new List<string>();
            names.Add((pt).ToString());
            names.AddRange(name);
            packet = new SPacket(EPacketType.Path, names.ToArray(), data.ToArray());
        }
        public PathedPacket(byte[] data)
        {
            packet = new SPacket(data);
        }
        public PathedPacket(SPacket packet)
        {
            this.packet = packet;
        }

        public EPathedPacketType RoutingType()
        {
            return Enum.Parse<EPathedPacketType>(Key.Split("/")[0]);
        }
        public string[] RoutingPath()
        {
            return Key.Split("/")[1..];
        }


        public Span<byte> Serialize()
        {
            return packet.Serialize();
        }

        static IPacket IPacket.DeserializePacket(Span<byte> data)
        {
            PathedPacket ret = new PathedPacket(data.ToArray());
            return ret;
        }
    }
}
