using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.net.Packet
{
    internal enum ESyncPacketType
    {
        ClientState = 0b00000000,
        PubSub = 0b00000001,
        Cache = 0b00000010,

    }
}
