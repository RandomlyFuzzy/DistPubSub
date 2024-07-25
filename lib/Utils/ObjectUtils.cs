using lib.net;
using lib.net.Packet;
using lib.serializer;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace lib.Utils
{
    public static class ObjectUtils
    {
        public static bool Eq(this byte[] lhs, byte[] rhs)
        {
            if (lhs.Length != rhs.Length)
            {
                return false;
            }

            for (int i = 0; i < lhs.Length; i++)
            {
                if (lhs[i] != rhs[i])
                {
                    return false;
                }
            }

            return true;

        }

        public static bool NotEq(this byte[] lhs, byte[] rhs)
        {
            return !lhs.Eq(rhs);
        }

        public static string GetFullType(this Type t)
        {
            if (t == typeof(void))
            {
                return "void";
            }

            if (t == null)
            {
                return "null";
            }

            //get the fullname of a type with a potential generic type
            if (t.IsGenericType)
            {
                string ret = t.FullName;
                ret = ret.Substring(0, ret.IndexOf('`'));
                ret += "<";
                foreach (Type arg in t.GetGenericArguments())
                {
                    ret += arg.GetFullType() + ",";
                }
                ret = ret.Substring(0, ret.Length - 1);
                ret += ">";
                return ret;
            }
            return t.FullName;
        }

        public static Type GetTypeFromName(this string t)
        {
            if (t == "void")
            {
                return typeof(void);
            }

            if (t == "null")
            {
                return null;
            }

            //get the type from a string with a potential generic type
            if (t.Contains("<"))
            {
                string name = t.Substring(0, t.IndexOf('<'));
                Type ret = Type.GetType(name);
                string[] args = t.Substring(t.IndexOf('<') + 1, t.Length - t.IndexOf('<') - 2).Split(',');
                Type[] argtypes = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    argtypes[i] = args[i].GetTypeFromName();
                }
                return ret.MakeGenericType(argtypes);
            }
            return Type.GetType(t);
        }
        static JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static byte[] SerializeObject(this object obj, Type t)
        {
            //serialize to JSON then to byte[]
            if(t.IsPrimitive || t == typeof(string))
            {
                return Encoding.UTF8.GetBytes(obj.ToString());
            }

            string json = JsonSerializer.Serialize(obj, t, options);
            return Encoding.UTF8.GetBytes(json);
        }

        public static object DeserializeObject(this byte[] data, Type t)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize(json, t);
        }

        static ArrayPool<byte> bufferes = ArrayPool<byte>.Shared;
        public static byte[] ReadBytes(this MemoryStream ms, int count)
        {
            byte[] ret = bufferes.Rent(count);
            ms.Read(ret,0,count);
            return ret;
        }

        public static byte[] ReadBytes(this MemoryStream ms, uint count) => ms.ReadBytes((int)count);

        public static void WriteBytes(this MemoryStream ms, Span<byte> data)
        {
            ms.Write(data);
        }

        public static uint ReadUint(this MemoryStream ms)
        {
            return SerializeUtils.DeserializeUint(ms.ReadBytes(4));
        }

        public static byte[] ReadBytes(this BufferLooper ms, int count)
        {
            byte[] ret = bufferes.Rent(count);
            ms.Read(ret, 0, count);
            return ret;
        }
        public static byte[] ReadBytes(this BufferLooper ms, uint count) => ms.ReadBytes((int)count);

        public static void WriteBytes(this BufferLooper ms, Span<byte> data)
        {
            ms.Write(data);
        }

        public static uint ReadUint(this BufferLooper ms)
        {
            return SerializeUtils.DeserializeUint(ms.ReadBytes(4));
        }
        public static string ToString(this EPathedPacketType type)
        {
            return ((int)type).ToString();
        }
    }
}
