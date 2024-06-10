using lib.net.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using lib.Routing;
using lib.serializer;
using lib.Utils;

namespace lib.net
{
    public class NetServer
    {
        public static NetServer Instance = null;
        protected TcpListener server;
        protected int port;
        protected List<NetClient> clients = new List<NetClient>();
        protected Dictionary<string, DateTime> heartBeat = new Dictionary<string, DateTime>();
        protected Timer timer;

        public NetServer(int port)
        {
            if (Instance == null)
            {
                Instance = this;
            }
            server = new TcpListener(port);
            this.port = port;
            Config.IsServer = true;
            if (Config.IsServer)
            {
                BindPathedPacketType();
            }
            //timer = new Timer(SendHeartbeat, null, 0, 1000);
        }

        ~NetServer()
        {
            if (Instance != null)
            {
                Instance = null;
            }
            Stop();
        }

        private void BindPathedPacketType()
        {
            string path = "";
            path = EPathedPacketType.Sub.ToString();
            PathRouting.AddPath((NetClient cli, object pp) => {
                PathedPacket pack = (PathedPacket)pp;
                string[] id = pack.RoutingPath();
                string path = String.Join("/", id);
                cli.ClientLog(path+" subscribed to");
                PathRouting.AddPath((NetClient cli2, object pp) =>
                {
                    PathedPacket pack = (PathedPacket)pp;
                    string[] id = pack.RoutingPath();
                    PathedPacket packet = new PathedPacket(EPathedPacketType.Pub, pack.Value, id);
                    //cli2.ClientLog("Publishing message to \"" + path + "\" subscribers");
                    cli.SendPacket(pack);
                    //PathRouting.InvokePath(cli,"", "Req"+ id);
                }, id);
            },path);

            path = EPathedPacketType.Pub.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                PathedPacket pack = (PathedPacket)pp;
                string[] id = pack.RoutingPath();
                PathRouting.InvokePath(cli, pp, id);
            }, path);
            path = EPathedPacketType.Uns.ToString();
            path = EPathedPacketType.Req.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                PathedPacket pack = (PathedPacket)pp;
                string[] id = pack.RoutingPath();
                KeyValueStore.Req(cli, id);
                //PathRouting.InvokePath(cli,"", "Req"+ id);
            }, path);
            path = EPathedPacketType.Res.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                PathedPacket pack = (PathedPacket)pp;
                var id = pack.RoutingPath();
                TypedPacket<object> packet = new(pack.Value);
                KeyValueStore.Set(id,packet);
                //PathRouting.InvokePath(cli,"", "Req"+ id);
            }, path);
            path = EPathedPacketType.Set.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                PathedPacket pack = (PathedPacket)pp;
                var id = pack.RoutingPath();
                TypedPacket<object> packet = new(pack.Value);
                Type t = packet.Key.GetTypeFromName();
                KeyValueStore.Set(id,packet.GetObject(t));
                //PathRouting.InvokePath(cli,"", "Req"+ id);
            }, path);
            path = EPathedPacketType.Rem.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                PathedPacket pack = (PathedPacket)pp;
                var id = pack.RoutingPath();
                KeyValueStore.Rem(id);
                //PathRouting.InvokePath(cli,"", "Req"+ id);
            }, path);

            path = EPathedPacketType.Err.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                PathedPacket pack = (PathedPacket)pp;
                TypedPacket<Exception> packet = new(pack.Value);
                Console.WriteLine(packet.GetObject(out _).Message);
            }, path);

            path = EPathedPacketType.Ack.ToString();
            path = EPathedPacketType.RAc.ToString();

            path = EPathedPacketType.Bro.ToString();

            path = EPathedPacketType.Pig.ToString();
            path = EPathedPacketType.Png.ToString();

            path = EPathedPacketType.Con.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                cli.ClientLog(" Connected");
                //PathRouting.InvokePath(cli,"", "Req"+ id);
            }, path);
            path = EPathedPacketType.Dis.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                cli.ClientLog(" Disconnected");
                cli.Close();
                //PathRouting.InvokePath(cli,"", "Req"+ id);
            }, path);

            path = EPathedPacketType.Hnd.ToString();
            path = EPathedPacketType.Hbt.ToString();
            PathRouting.AddPath((NetClient cli, object pp) => {
                PathedPacket pack = (PathedPacket)pp;
                var id = pack.RoutingPath();
                string id2 = String.Join("/",id);
                PathRouting.InvokePath(cli,"", "Heartbeat"+ id2);
            }, path);

            path = EPathedPacketType.Mes.ToString();

            path = EPathedPacketType.Com.ToString();
            path = EPathedPacketType.Evt.ToString();
            path = EPathedPacketType.Dat.ToString();
        }

        public void Start()
        {
            server.Start();
            ClientManager.OnClientConnect += Connect;
            ClientManager.OnClientDisconnect += Disconnect;
            server.BeginAcceptTcpClient(AcceptClient, null);
           
        }

        private void SendHeartbeat(object? obj)
        {
            if(clients.Count == 0)
            {
                return;
            }
            List<NetClient> temp;
            lock (clients)
            {
                temp = new List<NetClient>(clients);
            }
            foreach (var client in temp)
            {
                if(heartBeat.ContainsKey(client.ID) && heartBeat[client.ID]<DateTime.Now.Subtract(new TimeSpan(0,0,15)))
                {
                    Console.WriteLine($"Client {client.ID} has disconnected");
                    client.Close();
                    continue;
                }
                PathedPacket packet = new PathedPacket(EPathedPacketType.Hbt,client.ID.SerializeString(),client.ID);
                client.SendPacket(packet);
            }
        }

        private void AcceptClient(IAsyncResult ar)
        {
            TcpClient client = server.EndAcceptTcpClient(ar);
            NetClient netClient = new NetClient(client);
            Console.WriteLine($"Accepted Client {netClient.ip}:{netClient.port}");
            Instance.HeartBeat(netClient.ID);
            netClient.AddHeartbeatPath();
            clients.Add(netClient);
            server.BeginAcceptTcpClient(AcceptClient, null);
        }

        public void Connect(NetClient client)
        {
            lock(clients) 
                clients.Add(client);

            PathRouting.InvokePath(client, new PathedPacket(EPathedPacketType.Con, new byte[0], ""));
        }

        public void Disconnect(NetClient client)
        {
            lock(clients) 
                clients.Remove(client);

            PathRouting.InvokePath(client, new PathedPacket(EPathedPacketType.Dis, new byte[0], ""));

        }

        public void Stop()
        {
            server.Stop();
            ClientManager.OnClientConnect -= Connect;
            ClientManager.OnClientDisconnect -= Disconnect;
        }

        public void HeartBeat(string iD)
        {
            if (heartBeat.ContainsKey(iD))
            {
                heartBeat[iD] = DateTime.Now;
            }
            else
            {
                heartBeat.Add(iD, DateTime.Now);
            }
        }
    }
}
