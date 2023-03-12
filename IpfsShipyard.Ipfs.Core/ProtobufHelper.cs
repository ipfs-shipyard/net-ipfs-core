using System.Linq;
using System.Reflection;
using Google.Protobuf;

namespace IpfsShipyard.Ipfs.Core;

static class ProtobufHelper
{
    static MethodInfo _writeRawBytes = typeof(CodedOutputStream)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m =>
            m.Name == "WriteRawBytes" && m.GetParameters().Count() == 1
        );
    static MethodInfo _readRawBytes = typeof(CodedInputStream)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m =>
            m.Name == "ReadRawBytes"
        );

    public static void WriteSomeBytes(this CodedOutputStream stream, byte[] bytes)
    {
        _writeRawBytes.Invoke(stream, new object[] { bytes });
    }

    public static byte[] ReadSomeBytes(this CodedInputStream stream, int length)
    {
        return (byte[])_readRawBytes.Invoke(stream, new object[] { length });
    }
}