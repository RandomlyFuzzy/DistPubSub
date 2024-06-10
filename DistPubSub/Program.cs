using lib.net.Packet;
using lib.serializer;
using System.Text;

namespace DistPubSub
{
    internal class Program
    {
        static void Main(string[] args)
        {

            uint check = SerializeUtils.DeserializeUint(SerializeUtils.SerializeUint(1234));
            Console.WriteLine(check);
            NamedPacket packet = new NamedPacket("test", Encoding.ASCII.GetBytes("Hello World!"));
            var serialized = packet.Serialize();
            IPacket deserialized = NamedPacket.DeserializePacket(serialized);
            Console.WriteLine(deserialized.Equals(packet));
        }
    }
}
