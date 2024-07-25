namespace lib.net.Packet
{
    public enum EPathedPacketType
    {
        NOP = 0b11111111,
        Sub = 0b00000000,
        Pub = 0b00000001,
        Uns = 0b00000010,

        Req = 0b00000011,
        Res = 0b00000100,
        Set = 0b00000101,
        Rem = 0b00000110,

        Syn = 0b00000111,
        Ack = 0b00001000,
        RAc = 0b00001001,

        Bro = 0b00001010,

        Pig = 0b00001011,
        Png = 0b00001100,

        Con = 0b00001101,
        Dis = 0b00001110,

        Hnd = 0b00001111,

        Hbt = 0b00010000,

        Mes = 0b00010001,

        Com = 0b00010010,
        Evt = 0b00010011,
        Dat = 0b00010100,

        Err = 0b11111111,
    }
}
