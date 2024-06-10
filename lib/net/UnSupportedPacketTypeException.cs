using System.Runtime.Serialization;

namespace lib.net
{
    [Serializable]
    internal class UnSupportedPacketTypeException : Exception
    {

        public UnSupportedPacketTypeException():base("Unsupported packet type"){}

        UnSupportedPacketTypeException(string? message) : base(message)
        {
        }

        UnSupportedPacketTypeException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        UnSupportedPacketTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}