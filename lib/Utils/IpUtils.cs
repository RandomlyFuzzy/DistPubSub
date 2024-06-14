using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace lib.Utils
{
    public static class IpUtils
    {
        public static IPAddress GetLocalAddress(bool useFirst = false,bool Ipv4 = true)
        {
            IPAddress[] ipAddress = Dns.Resolve(Dns.GetHostName()).AddressList;
            if (Ipv4)
            {
                ipAddress = ipAddress.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToArray();
            }
            if (useFirst)
            {
                return ipAddress[0];
            }
            int i = 0;
            foreach (var item in ipAddress)
            {
                Console.WriteLine((i++) + " : " + item.ToString());
            }
            Console.WriteLine("Enter the index of the ip address you want to use");
            while (true)
            {
                string s = Console.ReadLine();
                if (int.TryParse(s, out i) && i >= 0 && i < ipAddress.Length)
                {
                    return ipAddress[i];
                }
                else
                {
                    Console.WriteLine("evaluation failed try a new index");
                }
            }
        }
    }
}
