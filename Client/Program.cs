using lib;
using lib.net;
using lib.Routing;
using lib.serializer;
using System.Numerics;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetClient client = new NetClient("127.0.0.1", 8080);
            //client.SubscribeToPath((ComplexTest i) => i.a=0, "mess");

            //var mess = GenerateMessage();
            //ComplexTest test = new ComplexTest();
            long id = 0;
            //KeyValueStore.Set("id", id);
            while (true)
            {
                //id = KeyValueStore.Get<int>("id").GetAwaiter().GetResult();
                //KeyValueStore.Set<int>("id", ++id);
                //KeyValueStore.Set<int>("id"+id, id);

                client.PublishToPath(id++, "mess");
                //client.SendPacket(new lib.net.Packet.NamedPacket("mess", mess));
            }
        }

        private static Span<byte> GenerateMessage()
        {
            //genearte giberish uptosize 1000

            byte[] ret = new byte[1000];
            int val = 1;
            for(int i = 0; i < ret.Length; i++)
            {
                ret[i] = (byte)(val % 256);
                val += ret[i];
                val += i;
            }
            return ret;

        }
    }
}
