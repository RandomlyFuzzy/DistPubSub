using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.net.Packet
{
    public enum EPacketType
    {
        Raw         = 0b00000000,
        Name        = 0b00000001,
        Path        = 0b00000010,
        Type        = 0b00000100,
        Sync        = 0b00001000,
        Reserviced1 = 0b00010000,
        Reserviced2 = 0b00100000,
        Reserviced3 = 0b01000000,
        Error       = 0b10000000
    }
}
