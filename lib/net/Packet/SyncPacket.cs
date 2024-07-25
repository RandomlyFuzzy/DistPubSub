using lib.serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.net.Packet
{
    internal class SyncPacket : IPacket
    {
        SPacket packet { get; init; }

        public EPacketType PacketType { get => packet.PacketType; }

        //updateType/ID...
        public string Key { get => packet.Key; }
        //PathToSource/PathToSource2/PathToSource3...
        public byte[] Value { get => packet.Value; }


        //PathedPacket() { }

        public SyncPacket(ESyncPacketType pt, string name) : this(pt, (new byte[0]).AsSpan(), name) { }

        public SyncPacket(ESyncPacketType pt, byte[] data, params string[] name) : this(pt, data.AsSpan(), name) { }

        public SyncPacket(ESyncPacketType pt, Span<Byte> data, params string[] name)
        {
            List<string> path = new (data.DeserializeString().Split("/"));
            if (!Config.IsServer)
            {
                path.Add(Config.CoreClient.LocalID);
            }
            else
            {
                path.Add(Config.CoreServer.ID);
            }

            List<string> names = new List<string>();
            names.Add((pt).ToString());
            names.AddRange(name);
            packet = new SPacket(EPacketType.Sync, names.ToArray(), data.ToArray());
        }

        public SyncPacket(byte[] data)
        {
            SPacket temp = new SPacket(data);
            string[] path = temp.Key.Split("/");
            if (path.Length == 0)
            {
                throw new Exception("Invalid packet header");
            }
            List<string> newPath = new List<string>(path);
            if (!Config.IsServer)
            {
                newPath.Add(Config.CoreClient.LocalID);
            }
            else
            {
                newPath.Add(Config.CoreServer.ID);
            }

            byte[] val = String.Join("/",newPath).SerializeString().ToArray();

            packet = new SPacket(temp.PacketType, temp.Key, val);
        }
        public SyncPacket(SPacket packet)
        {
            SPacket temp = packet;
            string[] path = temp.Value.DeserializeString().Split("/");
            if (path.Length == 0)
            {
                throw new Exception("Invalid packet header");
            }
            //incrment the path and append the local id
            List<string> newPath = new List<string>(path);
            if (!Config.IsServer)
            {
                newPath.Add(Config.CoreClient.LocalID);
            }
            else
            {
                newPath.Add(Config.CoreServer.ID);
            }
            byte[] val = String.Join("/", newPath).SerializeString().ToArray();

            this.packet = new SPacket(temp.PacketType, temp.Key, val);           
        }

        public ESyncPacketType RoutingType() => Enum.Parse<ESyncPacketType>(Key.Split("/")[0]);
        public string[] RoutingPath()=>Value.DeserializeString().Split("/");

        public int PacketLength() => packet.Value.DeserializeString().Split("/").Length;


        public Span<byte> Serialize() => packet.Serialize();

        static IPacket IPacket.DeserializePacket(Span<byte> data)
        {
            PathedPacket ret = new PathedPacket(data.ToArray());
            return ret;
        }
    }
}
