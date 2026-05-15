using lib.net.Packet;
using lib.Routing;
using lib.serializer;
using lib.Utils;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;

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
        public string LocalID => ((System.Net.IPEndPoint)client.Client.LocalEndPoint).Address.ToString() + ":" + ((System.Net.IPEndPoint)client.Client.LocalEndPoint).Port;

        public int Available => client.Available;
        public bool Connected => client.Connected;

        public int sent { get; private set; } = 0;
        public int received { get; private set; }
        public int Smessages { get; private set; } = 0;
        public int Rmessages { get; private set; } = 0;

        ConcurrentBag<byte[]> sendQueue;
        byte[] TempWriteBuffer;
        public bool HasSendData { get=> sendQueue!=null&&sendQueue.Count>0; }
        public int BuffersSent { get; private set; } = 0;
        public int PacketsTook { get; private set; } = 0;

        public NetClient(string ip, int port) : this(new TcpClient(ip, port))
        {

        }
        public NetClient(TcpClient client)
        {
            this.client = client;

            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false);

            this.ip = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            this.port = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Port;

            this.client.ReceiveBufferSize = 16 * (this.client.SendBufferSize = 2 << 16);
            buffer = new BufferLooper(this.client.ReceiveBufferSize * 2);

            ClientManager.AddClient(this);
            if (!Config.IsServer)
            {
                BindPathedPacketType();
                Config.CoreClient = this;
            }
            else
            {
                sendQueue = new ConcurrentBag<byte[]>();
                TempWriteBuffer = new byte[this.client.SendBufferSize];
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

        void Send(Span<byte> message)
        {
            sent += message.Length;
            if (!Connected)
                return;

            if (Config.IsServer && sendQueue.Count < 30000)
            {
                sendQueue.Add(message.ToArray());
                return;
            }
            Smessages++;
            try
            {
                lock (ns)
                {
                    ns.Write(message);
                }
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

        public (int?, byte[]) Receive()
        {
            if (!Connected || Available == 0)
                return (0, new byte[0]);

            Span<byte> data = pool.Rent(Available);
            int? len2 = ns?.Read(data);

            return (len2, data.ToArray());
        }

        BufferLooper buffer;
        public IEnumerable<SPacket> ReadPackets()
        {
            if (!client.Connected)
                yield break;

            var dat = Receive();
            if (dat.Item1 == null || dat.Item1 == 0)
            {
                pool.Return(dat.Item2);
                yield break;
            }
            received += dat.Item1.Value;

            lock (buffer)
                buffer.Write(dat.Item2, 0, dat.Item1.Value);
            pool.Return(dat.Item2);
            buffer.Position = 0;
            uint PacketLen = buffer.ReadUint();
            while (buffer.WriteLength - buffer.Position > PacketLen && PacketLen != 0)
            {
                yield return PacketFactory.NewPacket(ref buffer, PacketLen);
                Rmessages++;
                if (buffer.Length - buffer.Position < 4)
                    break;
                PacketLen = buffer.ReadUint();
            }
            int size = (int)buffer.WriteLength - (int)buffer.Position;
            if (size == 0)
                yield break;

            buffer.Position -= 4;
            byte[] data = buffer.ReadBytes(size + 4);
            buffer.Position = 0;
            buffer.WriteLength = 0;
            buffer.Write(data, 0, size + 4);
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

        public int ResetBuffersSent()
        {
            int ret = BuffersSent;
            BuffersSent = 0;
            return ret;
        }
        public int ResetPacketsTook()
        {
            int ret = PacketsTook;
            PacketsTook = 0;
            return ret;
        }


        List<(int, string[])> toRemove = new();
        public void Close()
        {
            client?.Close();
            client = null;
            if (Config.IsServer)
            {
                foreach (var item in toRemove)
                {
                    PathRouting.RemovePath(item.Item1, item.Item2);
                }
            }
        }

        public int InvokePath(object Data, params string[] path)
        {
            return PathRouting.InvokePath(this,Data, path);
        }
        public void BindPath(Action<object> action, params string[] path)
        {
            int id = PathRouting.AddPath((cli,obj)=>action(obj), path);
            toRemove.Add((id, path));
        }
        public void SubscribeToPath<T>(Action<T> action, params string[] path)
        {
            BindPath(obj => action((T)(new TypedPacket<T>(((PathedPacket)obj).Value).GetObject(typeof(T)))), path);
            SendPacket(new PathedPacket(EPathedPacketType.Sub, new byte[0], path));
            SendPacket(PathedPacket.NOP);
        }

        public void PublishToPath<T>(T data, params string[] path)
        {
            TypedPacket<T> pack = new TypedPacket<T>(data);
            SendPacket(new PathedPacket(EPathedPacketType.Pub, pack.Serialize(), path));
        }

        public void ClientLog(string message)
        {
            Console.WriteLine(ID+" "+message);
        }



        int size = 0;
        internal bool isServerConnection;

        internal void SendData()
        {
            while (ns != null && sendQueue.TryTake(out byte[] packet))
            {
                Smessages++;
                PacketsTook++;
                if (size + packet.Length > TempWriteBuffer.Length)
                {
                    BuffersSent++;
                    try
                    {
                        lock (ns)
                        {
                            ns?.Write(TempWriteBuffer, 0, size);
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    size = 0;
                }
                packet.CopyTo(TempWriteBuffer, size);
                size += packet.Length;
            }
        }
    }
}
