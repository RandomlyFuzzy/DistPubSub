using lib.net.Packet;
using lib.Routing;
using lib.serializer;
using lib.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace lib.net
{
    public class NetClient
    {
        protected TcpClient client;

        protected NetworkStream ns
        {
            get
            {
                if (!Connected)
                    return null;

                return client.GetStream();
            }
        }

        static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;

        public int port { get; init; }
        public string ip { get; init; }

        public string ID => ip + ":" + port;

        public int Available => client.Available;
        public bool Connected => client.Connected;

        public int sent { get; private set; } = 0;
        public int received { get; private set; }
        public int Smessages { get; private set; } = 0;
        public int Rmessages { get; private set; } = 0;

      


        public NetClient(string ip, int port) : this(new TcpClient(ip, port))
        {

        }
        public NetClient(TcpClient client)
        {
            this.client = client;

            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false);
            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false);
            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 8192);
            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 8192);




            this.ip = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            this.port = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Port;

            this.client.ReceiveBufferSize = 16 * (this.client.SendBufferSize = 8192);
            this.client.NoDelay = true;

            ClientManager.AddClient(this);
            if (!Config.IsServer)
            {
                BindPathedPacketType();
                Config.CoreClient = this;
            }

            PathRouting.InvokePath(this, new PathedPacket(EPathedPacketType.Con, new byte[0], ""));

        }

        private void BindPathedPacketType()
        {
            string path = EPathedPacketType.Hbt.ToString();
            PathRouting.AddPath((NetClient cli, object pp) => {
                PathedPacket pack = (PathedPacket)pp;
                string id = pack.Value.DeserializeString();
                //PathRouting.InvokePath(pack, "Heartbeat" + id);
                SendPacket(new PathedPacket(EPathedPacketType.Hbt, new byte[0], "Heartbeat" + id));
            }, path);

            path = EPathedPacketType.Pub.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                PathedPacket pack = (PathedPacket)pp;
                string[] id = pack.RoutingPath();
                PathRouting.InvokePath(cli, pp, id);
            }, path);

            path = EPathedPacketType.Res.ToString();
            PathRouting.AddPath((NetClient cli, object pp) =>
            {
                PathedPacket pack = (PathedPacket)pp;
                string[] id = pack.RoutingPath();
                PathRouting.InvokePath(cli, pp, id);
            }, path);
        }

        ~NetClient()
        {
            Close();
        }





        void Send(Span<byte> message){
            sent += message.Length;
            Smessages++;
            if(!Connected)
                return;

            using MemoryStream ms = new(message.ToArray());
            try
            {
                ms.WriteTo(ns);
                //ns?.Write(message);
                //ns?.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Source);
                Console.WriteLine(e.Message);
            }
        }
        public void Sendbyteblob(byte[] message)
        {
            Send(message);
        }

        public void SendPacket(SPacket np)
        {
            var data = np.Serialize();
            Send(data);
        }
        public void SendPacket(IPacket np)
        {
            var data = np.Serialize();
            Send(data);
        }

        public Span<byte> Receive()
        {
            if (!Connected)
                return new Span<byte>();
            
            int len = Available;
            Span<byte> data = stackalloc byte[len];
            int? len2 = ns?.Read(data);
            if (len2 == null)
                return new Span<byte>();

            if (len2 < len)
            {
                len = len2.Value;
            }
            return data.Slice(0, len).ToArray();
        }


        MemoryStream buffer = new MemoryStream(2<<15) ;
        public IEnumerable<SPacket> ReadPackets()
        {
            //recieve all the data
            if (!client.Connected)
                yield break;

            var dat = Receive();
            received += dat.Length;

            if(dat.Length == 0)
                yield break;

            buffer.Write(dat);

            buffer.Position = 0;
            uint PacketLen = buffer.ReadUint();
            while (buffer.Length - buffer.Position > PacketLen)
            {
                //var packet = buffer.ReadBytes(PacketLen);

                yield return PacketFactor.NewPacket(ref buffer,PacketLen);
                Rmessages++;
                //pool.Return(packet);
                if (buffer.Length - buffer.Position < 4)
                    break;
                PacketLen = buffer.ReadUint();
            }
            int size = (int)buffer.Length - (int)buffer.Position;
            if (size == 0)
                yield break;


            buffer.Position -= 4;
            byte[] data = buffer.ReadBytes(size+4);
            buffer.Position = 0;
            buffer = new MemoryStream(2<<15);
            buffer.Write(data,0,size+4);
            pool.Return(data);
        }


        public int ResetReceived()
        {
            int ret = received;
            received = 0;
            return ret;
        }
        public int ResetSent()
        {
            int ret = sent;
            sent = 0;
            return ret;
        }
        public int ResetSMessages()
        {
            int ret = Smessages;
            Smessages = 0;
            return ret;
        }
        public int ResetRMessages()
        {
            int ret = Rmessages;
            Rmessages = 0;
            return ret;
        }

        public void Close()
        {
            client?.Close();
            //ClientManager.RemoveClient(this);
            client = null;
        }

        public int InvokePath(object Data, params string[] path)
        {
            return PathRouting.InvokePath(this,Data, path);
        }
        public void BindPath(Action<object> action, params string[] path)
        {
            PathRouting.AddPath((cli,obj)=>action(obj), path);
        }
        public void SubscribeToPath<T>(Action<T> action,params string[] path)
        {
            BindPath(obj=>action((T)(new TypedPacket<T>(((PathedPacket)obj).Value).GetObject(typeof(T)))), path);
            SendPacket(new PathedPacket(EPathedPacketType.Sub,new byte[0], path));
            SendPacket(PathedPacket.NOP);
        }

        public void PublishToPath<T>(T data,params string[] path)
        {
            TypedPacket<T> pack = new TypedPacket<T>(data);
            SendPacket(new PathedPacket(EPathedPacketType.Pub, pack.Serialize(), path));
            //SendPacket(PathedPacket.NOP);
        }

        public void ClientLog(string message)
        {
            Console.WriteLine(ID+" "+message);
        }
    }
}
