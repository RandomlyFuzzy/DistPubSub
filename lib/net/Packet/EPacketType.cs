using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.net.Packet
{
    public enum EPacketType
    {
        Raw = 0b00000000,
        Name = 0b00000001,
        Path = 0b00000010,
        Type = 0b00000100,
        Error = 0b10000000
    }
}
