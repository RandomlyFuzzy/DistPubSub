using lib.net.Packet;
using lib.Utils;

namespace lib.net
{
    internal class PacketFactor
    {
        static int packetid = 0;
        //this method with create a packet from a buffer if the predefined packet parameters are not met it will attempt to recover the packet 
        public static SPacket NewPacket(ref MemoryStream buffer, uint packetLen)
        {
            buffer.Position -= 4;
            packetLen += 4;

            //byte[] data = buffer.ReadBytes((int)packetLen);
            SPacket packet = new SPacket(buffer);
            packetid++;
            return packet;
        }
    }
}