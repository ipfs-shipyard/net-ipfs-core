using IpfsShipyard.PeerTalk.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.PeerTalk.Tests.Protocols
{
    [TestClass]
    public class ProtocolRegistryTest
    {
        [TestMethod]
        public void PreRegistered()
        {
            CollectionAssert.Contains(ProtocolRegistry.Protocols.Keys, "/multistream/1.0.0");
            CollectionAssert.Contains(ProtocolRegistry.Protocols.Keys, "/plaintext/1.0.0");
        }

    }
}
