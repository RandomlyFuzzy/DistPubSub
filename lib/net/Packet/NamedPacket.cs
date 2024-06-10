using lib.serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace lib.net.Packet
{
    public readonly struct NamedPacket:IPacket
    {
        SPacket packet { get; init; }

        public EPacketType PacketType { get => packet.PacketType; }
        public string Key { get => packet.Key; }
        public byte[] Value { get => packet.Value; }


        public NamedPacket(string name, SPacket packet):this(name, packet.Serialize()) { }
        public NamedPacket(string name, byte[] data):this(name, data.AsSpan()){}
        public NamedPacket(string name, Span<Byte> data)
        {
            packet = new SPacket(EPacketType.Name, new string[] { name }, data.ToArray());
        }
        public NamedPacket(byte[] data)
        {
            packet = new SPacket(data);
        }
        public NamedPacket(SPacket packet)
        {
            this.packet = packet;
        }
        public Span<byte> Serialize()
        {
            return packet.Serialize();
        }

        public static IPacket DeserializePacket(Span<byte> data)
        {
            NamedPacket ret = new NamedPacket(data.ToArray());
            return ret;
        }
    }
}
