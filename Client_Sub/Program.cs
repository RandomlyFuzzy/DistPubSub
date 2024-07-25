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
            TimeSpan ts = DateTime.Now.Subtract(new DateTime(0));
/*            new Thread(() =>
            {
                while (true)
                {
                    TimeSpan ts2 = DateTime.Now.Subtract(new DateTime(0));

                    string str = "";

                    TimeSpan timeSpan = ts;
                    DateTime dt = new DateTime(0).Add(timeSpan);
                    str += ("Recieved time: " + dt.ToLongTimeString() + ":" + timeSpan.Milliseconds + ":" + timeSpan.Microseconds);

                    timeSpan = ts2;
                    dt = new DateTime(0).Add(timeSpan);
                    str+=("\t\tCurrent Time: "+ dt.ToLongTimeString() + ":"+timeSpan.Milliseconds+":"+timeSpan.Microseconds);

                    timeSpan = ts2.Subtract(ts) ;
                    dt = new DateTime(0).Add(timeSpan);
                    str += ("\t\tdiff Time: " + dt.ToLongTimeString() + ":" + timeSpan.Milliseconds + ":" + timeSpan.Microseconds);

                    Console.WriteLine(str);

                    Thread.Sleep(1000);
                }

            }).Start();*/
            client.SubscribeToPath((double i) => {

                ts = new TimeSpan((long)i * 10);
                //Console.WriteLine("Ticks: " + diff.ToString("N0")+" "+ticks.ToString("N0"));

                //Console.WriteLine("Diff: " + diff);

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
