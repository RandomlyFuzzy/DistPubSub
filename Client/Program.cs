using lib.net;
using lib.Utils;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ip = IpUtils.GetLocalAddress(args.Length > 0).ToString();
            int port = 8080;
            if (args.Length == 1)
            {
                ip = args[0];
                Console.WriteLine("setting ip to " + ip);
            }
            if (args.Length == 2)
            {
                port = int.Parse(args[1]);
                Console.WriteLine("Setting port to " + port);
            }
            Console.WriteLine("hello");
            NetClient client = new NetClient(ip, port);

            TimeSpan ts;
            while (true)
            {
                ts = DateTime.Now.Subtract(new DateTime(0));
                client.PublishToPath(ts.TotalMicroseconds, "mess");
            }
        }
    }
}
