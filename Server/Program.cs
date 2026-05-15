using lib.net;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetServer server = new NetServer(8080);
            server.Start();

            string input = "";
            while (input != "exit")
            {
                input = Console.ReadLine();
            }

            server.Stop();
        }
    }
}
