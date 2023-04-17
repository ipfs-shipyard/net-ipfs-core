using System.Linq;
using System.Reflection;
using Google.Protobuf;

namespace Ipfs
{
    internal static class ProtobufHelper
    {
        private static readonly MethodInfo WriteRawBytes = typeof(CodedOutputStream)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m =>
                m.Name == "WriteRawBytes" && m.GetParameters().Count() == 1
            );
        private static readonly MethodInfo ReadRawBytes = typeof(CodedInputStream)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m =>
                m.Name == "ReadRawBytes"
            );

        public static void WriteSomeBytes(this CodedOutputStream stream, byte[] bytes)
        {
            WriteRawBytes.Invoke(stream, new object[] { bytes });
        }

        public static byte[] ReadSomeBytes(this CodedInputStream stream, int length)
        {
            return (byte[])ReadRawBytes.Invoke(stream, new object[] { length });
        }
    }
}
