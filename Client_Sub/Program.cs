using lib.net;
using lib.Utils;

namespace Client_Sub
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ip = IpUtils.GetLocalAddress(args.Length >= 1).ToString();
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

            client.SubscribeToPath((double i) =>
            {
                ts = new TimeSpan((long)i * 10);
            }, "mess");

            Console.Read();
        }
    }
}
