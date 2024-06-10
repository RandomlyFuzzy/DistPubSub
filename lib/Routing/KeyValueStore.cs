using lib.net;
using lib.net.Packet;
using lib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace lib.Routing
{
    public static class KeyValueStore
    {
        static Dictionary<string, Type> TypeStore = new();
        static Dictionary<string, object> ObjStore = new();

        public static void Set<T>(string[] key, T value) => Set(String.Join("/",key), new TypedPacket<T>(value));
        public static void Set<T>(string key, T value) => Set(key, new TypedPacket<T>(value));


        public static void Set<T>(string key, TypedPacket<T> pack)
        {
            if (Config.IsServer)
            {
                Type type;
                var val = pack.GetObject(out type);
                if (!TypeStore.ContainsKey(key))
                {
                    TypeStore.Add(key, type);
                    ObjStore.Add(key, val);
                }
                else
                {
                    TypeStore[key] = type;
                    ObjStore[key] = val;
                }

                return;
            }
            else
            {
                PathedPacket pp = new(EPathedPacketType.Set, pack.Serialize(), key);
                Config.CoreClient.SendPacket(pp);
            }



        }
        public static void Rem(string[] key) => Rem(String.Join("/", key));
        public static void Rem(string key)
        {
            if (Config.IsServer)
            {
                TypeStore.Remove(key);
                ObjStore.Remove(key);
            }
            else
            {
                PathedPacket pp = new(EPathedPacketType.Rem, new byte[0], key);
                Config.CoreClient.SendPacket(pp);
            }
        }

        public static void Req(NetClient cli, string[] key) => Req(cli, String.Join("/", key));
        public static void Req(NetClient cli, string key)
        {
            if(!Config.IsServer)
            {
                return;
            }
            TypedPacket<object> packet = new(TypeStore[key], ObjStore[key]);
            PathedPacket pp = new(EPathedPacketType.Res, packet.Serialize(), key);
            cli.SendPacket(pp);
            cli.SendPacket(PathedPacket.NOP);
        }
        public static async Task<T> Get<T>(string[] key) => await Get<T>(String.Join("/", key));
        public static async Task<T> Get<T>(string key)
        {
            if (Config.IsServer)
            {
                if (ObjStore.ContainsKey(key))
                {
                    return (T)ObjStore[key];
                }
                return default;
            }
            TaskCompletionSource<T> tcs = new();
            int id = PathRouting.AddPath((NetClient cli, object pp) =>
            {
                PathedPacket pack = (PathedPacket)pp;
                //Console.WriteLine("\nrecieved Packet \n");
                TypedPacket<T> packet = new (pack.Value);
                tcs.SetResult((T)packet.GetObject(typeof(T)));
            }, key);
            PathedPacket pp = new(EPathedPacketType.Req, new byte[1] {0x30}, key);
            Config.CoreClient.SendPacket(pp);
            Config.CoreClient.SendPacket(PathedPacket.NOP);


            var ret = await tcs.Task;
            PathRouting.RemovePath(id, key);
            return ret;
        }





    }
}
