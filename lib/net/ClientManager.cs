using lib.net.Packet;
using lib.Routing;
using lib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

namespace lib.net
{
    public static class ClientManager
    {
        static List<NetClient> clients = new List<NetClient>();
        static Thread Reader = null;
        static Thread Writer = null;
        static Thread Stats = null;
        static CancellationTokenSource CancellationToken ;
        static int recieved = 0;
        static int Rmessages = 0;
        static int sent = 0;
        static int Smessages = 0;
        static int buffersSent = 0;
        static int packetsTook = 0;

        public static event Action<NetClient> OnClientConnect;
        public static event Action<NetClient> OnClientDisconnect;




        public static void AddClient(NetClient client)
        {
            lock (clients)
            clients.Add(client);
            OnClientConnect?.Invoke(client);




            if (Reader == null||!Reader.IsAlive)
            {
                if (CancellationToken != null)
                {
                    CancellationToken.Cancel();
                }
                CancellationToken = new CancellationTokenSource();
                Reader = new Thread(ReadClients);
                Reader.Start();
            }
            if (Stats == null || !Stats.IsAlive)
            {
                Stats = new Thread(ReadStats);
                Stats.Start();
            }
            //TODO: Fix this
            if (Config.IsServer)
            {
                if (Writer == null || !Writer.IsAlive)
                {
                    Writer = new Thread(WriteClients);
                    Writer.Start();
                }
            }
        }

        private static void WriteClients()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                List<NetClient> clients2;
                lock (clients)
                {
                    clients2 = new List<NetClient>(clients);
                }
                Parallel.ForEach(clients2, client =>
                {
                    if (!client.Connected)
                    {
                        RemoveClient(client);
                        return;
                    }
                    if (!client.HasSendData)
                    {
                        return;
                    }
                    client.SendData();
                });
                Thread.Sleep(1);
            }
            Console.WriteLine("Write Thread Aborted");
        }

        private static void ReadStats()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            Console.WriteLine("");
           

            while (!CancellationToken.IsCancellationRequested)
            {

                //TcpStatistics tcpstat = properties.GetTcpIPv4Statistics();
                recieved = clients.Sum(client => client.ResetReceived());
                Rmessages = clients.Sum(client => client.ResetRMessages());
                sent = clients.Sum(client => client.ResetSent());
                Smessages = clients.Sum(client => client.ResetSMessages());
                Console.WriteLine($"      Clients:                              : {clients.Count}");
                Console.WriteLine($"      Recieved:                             : {recieved}");
                Console.WriteLine($"      Recieved Messages:                    : {Rmessages}");
                Console.WriteLine($"      Sent Bytes:                           : {sent}");
                Console.WriteLine($"      Sent Messages:                        : {Smessages}");
                if(Config.IsServer)
                {
                    buffersSent = clients.Sum(client => client.ResetBuffersSent());
                    Console.WriteLine($"      Buffers Sent:                         : {buffersSent}");
                    packetsTook = clients.Sum(client => client.ResetPacketsTook());
                    Console.WriteLine($"      Packets Took:                         : {packetsTook}");

                }

                //Console.WriteLine("  Minimum Transmission Timeout............. : {0}",tcpstat.MinimumTransmissionTimeout);
                //Console.WriteLine("  Maximum Transmission Timeout............. : {0}",tcpstat.MaximumTransmissionTimeout);
                //Console.WriteLine("  Connection Data:");
                //Console.WriteLine("      Current  ............................ : {0}",tcpstat.CurrentConnections);
                //Console.WriteLine("      Cumulative .......................... : {0}",tcpstat.CumulativeConnections);
                //Console.WriteLine("      Initiated ........................... : {0}",tcpstat.ConnectionsInitiated);
                //Console.WriteLine("      Accepted ............................ : {0}",tcpstat.ConnectionsAccepted);
                //Console.WriteLine("      Failed Attempts ..................... : {0}",tcpstat.FailedConnectionAttempts);
                //Console.WriteLine("      Reset ............................... : {0}",tcpstat.ResetConnections);
                //Console.WriteLine("");
                //Console.WriteLine("  Segment Data:");
                //Console.WriteLine("      Received  ........................... : {0}",tcpstat.SegmentsReceived);

                //Console.WriteLine("      Sent ................................ : {0}",tcpstat.SegmentsSent);

                //Console.WriteLine("      Retransmitted ....................... : {0}",tcpstat.SegmentsResent);

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
                                client.InvokePath(pathedPacket, pathedPacket.RoutingType().ToString());
                                break;
                            case EPacketType.Type:
                                Type t = TypedPacket<object>.GetPathedType(item.Serialize());
                                client.InvokePath((new TypedPacket<object>(item)).GetObject(t), t.GetFullType());
                                break;
                            case EPacketType.Sync:
                                SyncPacket syncPacket = new SyncPacket(item);
                                SyncPacketRouter.Route(syncPacket, client);
                                break;
                            case EPacketType.Reserviced1:
                            case EPacketType.Reserviced2:
                            case EPacketType.Reserviced3:
                            case EPacketType.Error:
                            default:
                                Console.WriteLine("invalid packet type "+item.PacketType);
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
