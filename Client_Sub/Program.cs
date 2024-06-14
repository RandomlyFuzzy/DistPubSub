using lib.net;
using lib.Routing;
using lib;
using lib.Utils;

namespace Client_Sub
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ip = IpUtils.GetLocalAddress(args.Length>=1).ToString();
            int port = 8080;
            if (args.Length == 1)
            {
                ip = args[0];
            }
            if (args.Length == 2)
            {
                port = int.Parse(args[1]);
            }
            NetClient client = new NetClient(ip, port);
            client.SubscribeToPath((long i) => {

                //if(i % 10000 != 0)
                //{
                //    return;
                //}
                //Console.WriteLine(i);
                i = 0;
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
