using lib.net;
using lib.Routing;
using lib;

namespace Client_Sub
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetClient client = new NetClient("127.0.0.1", 8080);
            client.SubscribeToPath((long i) => { 
                
                if(i % 10000 != 0)
                {
                    return;
                }
                Console.WriteLine(i);

            }, "mess");

            Console.Read();

            //var mess = GenerateMessage();
            //ComplexTest test = new ComplexTest();
            //int id = 0;
            //KeyValueStore.Set("id", id);
            //while (true)
            //{
            //    //id = KeyValueStore.Get<int>("id").GetAwaiter().GetResult();
            //    //KeyValueStore.Set<int>("id", ++id);
            //    //KeyValueStore.Set<int>("id" + id, id);

            //    client.PublishToPath(mess, "mess");
            //    //client.SendPacket(new lib.net.Packet.NamedPacket("mess", mess));
            //}
        }
    }
}
