using lib.net.Packet;
using lib.serializer;
using lib.Routing;
using lib.Utils;
using System.Text;

namespace DistPubSub.Tests
{
    public class PacketTests
    {
        [Fact]
        public void SerializeUtils_RoundTrip_Uint()
        {
            uint value = 1234;
            var serialized = SerializeUtils.SerializeUint(value);
            var deserialized = SerializeUtils.DeserializeUint(serialized);
            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void SerializeUtils_RoundTrip_String()
        {
            string value = "Hello World!";
            var serialized = value.SerializeString();
            var deserialized = serialized.DeserializeString();
            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void SerializeUtils_EmptyString()
        {
            string value = "";
            var serialized = value.SerializeString();
            var deserialized = serialized.DeserializeString();
            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void NamedPacket_SerializationRoundTrip()
        {
            NamedPacket packet = new NamedPacket("test", Encoding.ASCII.GetBytes("Hello World!"));
            var serialized = packet.Serialize();
            IPacket deserialized = NamedPacket.DeserializePacket(serialized);
            Assert.True(deserialized.Equals(packet));
        }

        [Fact]
        public void NamedPacket_KeyAndValue()
        {
            NamedPacket packet = new NamedPacket("mykey", new byte[] { 1, 2, 3, 4 });
            Assert.Equal("mykey", packet.Key);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, packet.Value);
        }

        [Fact]
        public void PathedPacket_RoundTrip()
        {
            PathedPacket packet = new PathedPacket(EPathedPacketType.Pub, new byte[] { 0x01, 0x02 }, "test", "path");
            var serialized = packet.Serialize();
            PathedPacket deserialized = new PathedPacket(serialized.ToArray());
            Assert.Equal(packet.RoutingType(), deserialized.RoutingType());
            Assert.Equal(packet.Value.ToArray(), deserialized.Value.ToArray());
        }

        [Fact]
        public void PathedPacket_RoutingTypeAndPath()
        {
            PathedPacket packet = new PathedPacket(EPathedPacketType.Sub, new byte[0], "mess");
            Assert.Equal(EPathedPacketType.Sub, packet.RoutingType());
            Assert.Equal(new[] { "mess" }, packet.RoutingPath());
        }

        [Fact]
        public void PathedPacket_RoutingPathMultipleSegments()
        {
            PathedPacket packet = new PathedPacket(EPathedPacketType.Pub, new byte[0], "a", "b", "c");
            Assert.Equal(EPathedPacketType.Pub, packet.RoutingType());
            Assert.Equal(new[] { "a", "b", "c" }, packet.RoutingPath());
        }

        [Fact]
        public void TypedPacket_RoundTrip_Int()
        {
            TypedPacket<int> packet = new TypedPacket<int>(42);
            var serialized = packet.Serialize();
            TypedPacket<int> deserialized = new TypedPacket<int>(serialized.ToArray());
            Assert.Equal(42, deserialized.GetObject(out _));
        }

        [Fact]
        public void TypedPacket_RoundTrip_Double()
        {
            TypedPacket<double> packet = new TypedPacket<double>(3.14159);
            var serialized = packet.Serialize();
            TypedPacket<double> deserialized = new TypedPacket<double>(serialized.ToArray());
            Assert.Equal(3.14159, deserialized.GetObject(out _));
        }

        [Fact]
        public void SPacket_KeyValue()
        {
            SPacket packet = new SPacket(EPacketType.Name, "mykey", new byte[] { 10, 20, 30 });
            Assert.Equal(EPacketType.Name, packet.PacketType);
            Assert.Equal("mykey", packet.Key);
            Assert.Equal(new byte[] { 10, 20, 30 }, packet.Value);
        }

        [Fact]
        public void SPacket_ArrayKey()
        {
            SPacket packet = new SPacket(EPacketType.Path, new[] { "a", "b", "c" }, new byte[0]);
            Assert.Equal("a/b/c", packet.Key);
        }

        [Fact]
        public void SPacket_SerializeDeserialize()
        {
            SPacket original = new SPacket(EPacketType.Name, "key", new byte[] { 1, 2, 3, 4, 5 });
            var data = original.Serialize();
            SPacket deserialized = new SPacket(data.ToArray());
            Assert.Equal(original.PacketType, deserialized.PacketType);
            Assert.Equal(original.Key, deserialized.Key);
            Assert.Equal(original.Value, deserialized.Value);
        }

        [Fact]
        public void SPacket_EmptyValue()
        {
            SPacket original = new SPacket(EPacketType.Name, "empty", new byte[0]);
            var data = original.Serialize();
            SPacket deserialized = new SPacket(data.ToArray());
            Assert.Equal("empty", deserialized.Key);
            Assert.Empty(deserialized.Value);
        }

        [Fact]
        public void EPacketType_Values()
        {
            Assert.Equal(0b00000000, (int)EPacketType.Raw);
            Assert.Equal(0b00000001, (int)EPacketType.Name);
            Assert.Equal(0b00000010, (int)EPacketType.Path);
            Assert.Equal(0b00000100, (int)EPacketType.Type);
            Assert.Equal(0b00001000, (int)EPacketType.Sync);
            Assert.Equal(0b10000000, (int)EPacketType.Error);
        }

        [Fact]
        public void ObjectUtils_ByteEquality()
        {
            byte[] a = { 1, 2, 3 };
            byte[] b = { 1, 2, 3 };
            byte[] c = { 1, 2, 4 };
            Assert.True(a.Eq(b));
            Assert.False(a.Eq(c));
        }

        [Fact]
        public void ObjectUtils_GetFullType_Simple()
        {
            Assert.Equal("System.Int32", typeof(int).GetFullType());
            Assert.Equal("System.String", typeof(string).GetFullType());
            Assert.Equal("System.Double", typeof(double).GetFullType());
        }

        [Fact]
        public void ObjectUtils_GetTypeFromName_Simple()
        {
            Assert.Equal(typeof(int), "System.Int32".GetTypeFromName());
            Assert.Equal(typeof(string), "System.String".GetTypeFromName());
            Assert.Equal(typeof(double), "System.Double".GetTypeFromName());
        }
    }
}
