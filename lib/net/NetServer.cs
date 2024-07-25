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
using System.Security.Cryptography.X509Certificates;
using System.Security.Claims;

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

        public string IP { get; private set; }
        public int Port { get { return port; } }

        public string ID { get => IP + ":" + Port; }

        public NetServer(int port) : this(port, null)
        { }
        NetServer(int port, string MasterIpAddress = "", int masterPort = -1) : this(port, MasterIpAddress != "" && masterPort != -1 ? new TcpClient(MasterIpAddress, masterPort) : null)
        { }
        NetServer(int port, TcpClient MasterServer = null)
        {
            if (Instance == null)
            {
                Instance = this;
            }

            IPAddress address = IpUtils.GetLocalAddress();

            IP = address.ToString();
            
            server = new TcpListener(address,port);
            this.port = port;
            Config.IsServer = true;
            Config.CoreServer = this;
            BindPathedPacketType();
            Console.WriteLine(address.ToString()) ;
            if(MasterServer != null)
            {
                NetClient client = new NetClient(MasterServer);
                client.isServerConnection = true;
                PathedPacket packet = new PathedPacket(EPathedPacketType.Bro, new byte[0], "");
                client.SendPacket(packet);
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
                ClientManager.RemoveClient(cli);
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

        private void AcceptClient(IAsyncResult ar)
        {
            TcpClient client = server.EndAcceptTcpClient(ar);
            NetClient netClient = new NetClient(client);
            Console.WriteLine($"Accepted Client {netClient.ip}:{netClient.port}");
            Instance.HeartBeat(netClient.ID);
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
