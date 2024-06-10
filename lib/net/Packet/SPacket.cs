using lib.serializer;
using lib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace lib.net.Packet
{
    public readonly struct SPacket
    {
        public int HeaderLength { get => 4+4+4+1+7; }
        const string tail = "|||||||";//7bytes
        public int Length { get => HeaderLength + 4 + Key.Length + Value.Length; }
        public EPacketType PacketType { get; internal init; }
        public string Key { get; init; }
        public byte[] Value { get; init; }


        public SPacket(EPacketType packetType, string key, byte[] value)
        {
            PacketType = packetType;
            Key = key;
            Value = value;
        }
        public SPacket(EPacketType packetType, string[] key, byte[] value)
        {
            PacketType = packetType;
            Key = string.Join("/", key);
            Value = value;
        }

        public SPacket(byte[] data)
        {
            byte[] Header = data[..HeaderLength];
            int pos = 0;
            //read length
            int length = (int)SerializeUtils.DeserializeUint(Header[..4]);
            pos += 4;
            PacketType = (EPacketType)Header[pos];
            pos += 1;
            uint keyLen = Header[pos..].DeserializeUint();
            pos += 4;
            uint valueLen = Header[pos..].DeserializeUint();
            pos += 4;
            string tail = string.Join("", Header[pos..].Select(a => (char)a));
            if (tail != "|||||||")
            {
                throw new Exception("Invalid packet header");
            }
            byte[] body = data[HeaderLength..]; 
            Key = body[..(int)keyLen].DeserializeString();
            if (valueLen == 0)
            {
                Value = new byte[0];
                return;
            }
            Value = body[(int)keyLen..(int)(keyLen+valueLen)].ToArray();
        }

        public SPacket(MemoryStream stream)
        {
            byte[] Header = stream.ReadBytes(HeaderLength);

            int pos = 0;
            //read length
            int length = (int)SerializeUtils.DeserializeUint(Header[..4]);
            pos += 4;
            PacketType = (EPacketType)Header[pos];
            pos += 1;
            uint keyLen = Header[pos..].DeserializeUint();
            pos += 4;
            uint valueLen = Header[pos..].DeserializeUint();
            pos += 4;
            string tail = string.Join("",Header[pos..20].Select(a=>(char)a));
            if (tail != "|||||||")
            {
                throw new Exception("Invalid packet header");
            }
            //stream.Position -= (Header.Length - 20);
            Key = stream.ReadBytes((int)keyLen).DeserializeString();
            Value = stream.ReadBytes((int)valueLen).ToArray()[..(int)valueLen];

            //byte[] data = stream.ReadBytes((int)stream.Length);
            //int pos = 0;

            //PacketType = (EPacketType)data[pos];
            //pos += 1;
            //Key = SerializeUtils.DeserializeString(data[pos..]);
            //pos += 4 + Key.Length;
            //int len = (int)SerializeUtils.DeserializeUint(data[pos..]);
            //pos += 4;
            //Value = data[pos..(pos + len)].ToArray();
        }   

        public Span<byte> Serialize()
        {
            Span<byte> ret = stackalloc byte[Length];
            //total length
            SerializeUtils.SerializeUint((uint)Length).CopyTo(ret);
            //packet type
            ret[4] = ((byte)PacketType);
            //key length
            int keyLength = Key.Length+4;
            SerializeUtils.SerializeUint((uint)(keyLength)).CopyTo(ret.Slice(5));
            //value length
            SerializeUtils.SerializeUint((uint)Value.Length).CopyTo(ret.Slice(5 + 4));
            //tail of header
            tail.ToCharArray().Select(a=>(byte)a).ToArray().CopyTo(ret.Slice(5 + 4 + 4));

            //data
            Key.SerializeString().CopyTo(ret.Slice(20));
            if(Value.Length != 0)
            {
                Value.CopyTo(ret.Slice(20 + 4 + Key.Length ));
            }
            return ret.ToArray();
        }

        //public void SetKey(params string[] keys)
        //{
        //    Key = string.Join("/", keys);
        //}
        public string[] GetKey()
        {
            return Key.Split("/");
        }

        public bool Equals(object? obj)
        {
            if (obj is SPacket)
            {
                SPacket packet = (SPacket)obj;
                bool ret = true;
                ret &= Key == packet.Key;
                ret &= Value.Eq(packet.Value);
                return ret;
            }
            return false;
        }

        public Type GetType()
        {
            switch (PacketType)
            {
                case EPacketType.Name:
                    return typeof(NamedPacket);
                case EPacketType.Path:
                    return typeof(PathedPacket);
                case EPacketType.Type:
                    return typeof(TypedPacket<>);
                default:
                    return typeof(SPacket);
            }
            return typeof(SPacket);
        }
    }
}
