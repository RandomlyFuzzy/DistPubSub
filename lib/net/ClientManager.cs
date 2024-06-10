using lib.net.Packet;
using lib.Routing;
using lib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.net
{
    public static class ClientManager
    {
        static List<NetClient> clients = new List<NetClient>();
        static Thread Reader = null;
        static Thread Stats = null;
        static CancellationTokenSource CancellationToken ;
        static int recieved = 0;
        static int Rmessages = 0;
        static int sent = 0;
        static int Smessages = 0;

        public static event Action<NetClient> OnClientConnect;
        public static event Action<NetClient> OnClientDisconnect;




        public static void AddClient(NetClient client)
        {
            lock (clients)
            clients.Add(client);
            OnClientConnect?.Invoke(client);
            if(Reader == null||!Reader.IsAlive)
            {
                if(CancellationToken != null)
                {
                    CancellationToken.Cancel();
                }
                CancellationToken = new CancellationTokenSource();
                if (Reader == null)
                {
                    Stats = new Thread(ReadStats);
                    Stats.Start();
                }

                Reader = new Thread(ReadClients);
                Reader.Start();

               
            }
        }

        private static void ReadStats()
        {
            while(!CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Clients: {clients.Count}");

                recieved = clients.Sum(client => client.ResetReceived());
                Console.WriteLine($"Recieved: {recieved}");

                Rmessages = clients.Sum(client => client.ResetRMessages());
                Console.WriteLine($"Recieved Messages: {Rmessages}");

                sent = clients.Sum(client => client.ResetSent());
                Console.WriteLine($"Sent: {sent}");

                Smessages = clients.Sum(client => client.ResetSMessages());
                Console.WriteLine($"Sent Messages: {Smessages}");

                recieved = 0;
                Thread.Sleep(1000);
            }
        }


        private static void ReadClients(object? obj)
        {
            Thread.Sleep(10);
            Console.WriteLine("begining read");
            bool read = false;
            while(!CancellationToken.IsCancellationRequested)
            {
                read = false;
                List<NetClient> clients2;
                lock (clients)
                {
                    clients2 = new List<NetClient>(clients);
                }

                Parallel.ForEach(clients2, client =>
                //foreach (NetClient client in clients2)
                {
                    if (!client.Connected)
                    {
                        RemoveClient(client);
                        return;
                    }
                    if (client.Available == 0)
                    {
                        return;
                    }
                    read = true;
                    foreach (SPacket item in client.ReadPackets())
                    {
                        //Console.Write("\n"+item.PacketType+"/");
                        switch (item.PacketType)
                        {
                            case EPacketType.Raw:
                                client.InvokePath(item.Value, item.Key);
                                break;
                            case EPacketType.Name:
                                NamedPacket packet = new NamedPacket(item);
                                client.InvokePath(packet.Value, packet.Key);
                                break;
                            case EPacketType.Path:
                                PathedPacket pathedPacket = new PathedPacket(item);
                                //Console.WriteLine(pathedPacket.Key+"\n");
                                //int activePath = client.InvokePath(pathedPacket, pathedPacket.RoutingPath());
                                //if (activePath == 0)
                                client.InvokePath(pathedPacket, pathedPacket.RoutingType().ToString());
                                break;
                            case EPacketType.Type:
                                Type t = TypedPacket<object>.GetPathedType(item.Serialize());
                                client.InvokePath((new TypedPacket<object>(item)).GetObject(t), t.GetFullType());
                                break;
                            default:
                                break;
                        }
                    }
                }
                );
                if (!read)
                {
                    Thread.Sleep(1);
                }
            }
            Console.WriteLine("Read Thread Aborted");
        }

        public static void RemoveClient(NetClient client)
        {
            lock (clients)
            clients.Remove(client);
            OnClientDisconnect?.Invoke(client);
            if(clients.Count == 0)
            {
                CancellationToken.Cancel();
                Reader = null;
                Stats = null;
            }
        }
    }
}
