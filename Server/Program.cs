using lib.net;
using System.Net;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {


            //string MasterIpAddress = "";
            //int MasterPort = -1;
            //if(args.Length == 2)
            //{
            //    MasterIpAddress = args[0];
            //    MasterPort = int.Parse(args[1]);
            //}

            NetServer server = new NetServer(8080);//,MasterIpAddress,MasterPort);
            server.Start();

            string input = "";
            while(input != "exit")
            {
                input = Console.ReadLine();
            }

            server.Stop();
        }
    }
}
