using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PeerTalk.Protocols;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    [TestClass]
    public class AutoDialerTest
    {
        Peer peerA = new Peer
        {
            AgentVersion = "A",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };
        Peer peerB = new Peer
        {
            AgentVersion = "B",
            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
        };

        [TestMethod]
        public void Defaults()
        {
            using (var dialer = new AutoDialer(new Swarm()))
            {
                Assert.AreEqual(AutoDialer.DefaultMinConnections, dialer.MinConnections);
            }
        }

        [TestMethod]
        public async Task Connects_OnPeerDiscover_When_Below_MinConnections()
        {
            var swarmA = new Swarm { LocalPeer = peerA };
            await swarmA.StartAsync();
            var peerAAddress = await swarmA.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarmB = new Swarm { LocalPeer = peerB };
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            try
            {
                using (var dialer = new AutoDialer(swarmA))
                {
                    var other = await swarmA.RegisterPeerAsync(peerBAddress);

                    // wait for the connection.
                    var endTime = DateTime.Now.AddSeconds(3);
                    while (other.ConnectedAddress == null)
                    {
                        if (DateTime.Now > endTime)
                            Assert.Fail("Did not do autodial");
                        await Task.Delay(100);
                    }
                }
            }
            finally
            {
                await swarmA?.StopAsync();
                await swarmB?.StopAsync();
            }
        }

        [TestMethod]
        public async Task Noop_OnPeerDiscover_When_NotBelow_MinConnections()
        {
            var swarmA = new Swarm { LocalPeer = peerA };
            await swarmA.StartAsync();
            var peerAAddress = await swarmA.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarmB = new Swarm { LocalPeer = peerB };
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            try
            {
                using (var dialer = new AutoDialer(swarmA) { MinConnections = 0 })
                {
                    var other = await swarmA.RegisterPeerAsync(peerBAddress);

                    // wait for the connection.
                    var endTime = DateTime.Now.AddSeconds(3);
                    while (other.ConnectedAddress == null)
                    {
                        if (DateTime.Now > endTime)
                            return;
                        await Task.Delay(100);
                    }
                    Assert.Fail("Autodial should not happen");
                }
            }
            finally
            {
                await swarmA?.StopAsync();
                await swarmB?.StopAsync();
            }
        }
    }
}
