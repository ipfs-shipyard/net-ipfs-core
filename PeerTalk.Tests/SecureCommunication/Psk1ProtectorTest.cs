using System.IO;
using System.Threading.Tasks;
using IpfsShipyard.PeerTalk.Cryptography;
using IpfsShipyard.PeerTalk.SecureCommunication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.PeerTalk.Tests.SecureCommunication
{
    [TestClass]
    public class Psk1ProtectorTest
    {
        [TestMethod]
        public async Task Protect()
        {
            var psk = new PreSharedKey().Generate();
            var protector = new Psk1Protector { Key = psk };
            var connection = new PeerConnection { Stream = Stream.Null };
            var protectedStream = await protector.ProtectAsync(connection);
            Assert.IsInstanceOfType(protectedStream, typeof(Psk1Stream));
        }

    }
}
