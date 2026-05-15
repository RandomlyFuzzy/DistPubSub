using lib.serializer;
using lib.Utils;
using System;

namespace lib.net.Packet
{
    public struct TypedPacket<T> :IPacket
    {

        SPacket packet { get; init; }

        public EPacketType PacketType { get => packet.PacketType; }
        public string Key { get => packet.Key; }
        public byte[] Value { get => packet.Value; }


        public TypedPacket(T data) : this(data.GetType(),data) { }
        public TypedPacket(Type t, object data)
        {
            packet = new SPacket(EPacketType.Type, new string[] { t.GetFullType() }, data.SerializeObject(t));
        }

        public TypedPacket(byte[] data)
        {
            packet = new SPacket(data);
        }
        public TypedPacket(SPacket packet)
        {
            this.packet = packet;
        }


        public T GetObject(out Type t)
        {

            t = Key.GetTypeFromName();
            return (T)Value.DeserializeObject(t);
        }

        public object GetObject(Type t)
        {
            return Value.DeserializeObject(t);
        }

        public static Type GetPathedType(Span<byte> data)
        {
            return data[5..].DeserializeString().GetTypeFromName();
        }

        public Span<byte> Serialize()
        {
            return packet.Serialize();
        }

        static IPacket IPacket.DeserializePacket(Span<byte> data)
        {
            TypedPacket<T> ret = new TypedPacket<T>(data.ToArray());
            return ret;
        }
    }
}
