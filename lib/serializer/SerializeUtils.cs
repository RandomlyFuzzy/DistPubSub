using System;
using System.Buffers.Binary;
using System.Text;

namespace lib.serializer
{
    public static class SerializeUtils
    {

        public static byte[] SerializeUint(this uint value)
        {
            byte[] ret = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(ret, value);
            return ret;
        }

        public static uint DeserializeUint(this Span<byte> data)
        {
            if (data.Length < 4)
            {
                throw new Exception("Invalid uint32 length");
            }
            return BinaryPrimitives.ReadUInt32BigEndian(data);
        }

        public static uint DeserializeUint(this byte[] data)
        {
            return DeserializeUint(new Span<byte>(data));
        }

        public static Span<byte> SerializeString(this string str)
        {
            //get the length of the string and serialize it as a uint first
            //then serialize the string as bytes

            if(str.Length == 0)
            {
                return new byte[0];
            }

            Span<byte> ret = stackalloc byte[4 + str.Length];
            SerializeUint((uint)str.Length).CopyTo(ret);
            Encoding.ASCII.GetBytes(str).CopyTo(ret.Slice(4));
            return ret.ToArray();
        }

        public static string DeserializeString(this Span<byte> data)
        {
            //deserialize the length of the string as a uint
            //then deserialize the string as bytes
            if(data.Length == 0)
            {
                return "";
            }
            uint length = DeserializeUint(data);
            return Encoding.ASCII.GetString(data.ToArray(), 4, (int)length);
        }

        public static string DeserializeString(this byte[] data)
        {
            return data.AsSpan().DeserializeString();
        }

    }
}
