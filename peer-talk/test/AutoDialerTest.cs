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

        Peer peerC = new Peer
        {
            AgentVersion = "C",
            Id = "QmTcEBjSTSLjeu2oTiSoBSQQgqH5MADUsemXewn6rThoDT",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCAL8J1Lp6Ad5eYanOwNenXZ6Efvhk9wwFRXqqPn9UT+/JTxBvZPzQwK/FbPRczjZ/A1x8BSec1gvFCzcX4fkULAgMBAAE="
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
        public async Task Connects_OnPeerDiscovered_When_Below_MinConnections()
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
        public async Task Noop_OnPeerDiscovered_When_NotBelow_MinConnections()
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

        [TestMethod]
        public async Task Connects_OnPeerDisconnected_When_Below_MinConnections()
        {
            var swarmA = new Swarm { LocalPeer = peerA };
            await swarmA.StartAsync();
            var peerAAddress = await swarmA.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarmB = new Swarm { LocalPeer = peerB };
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarmC = new Swarm { LocalPeer = peerC };
            await swarmC.StartAsync();
            var peerCAddress = await swarmC.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            bool isBConnected = false;
            swarmA.ConnectionEstablished += (s, conn) =>
            {
                if (conn.RemotePeer == peerB)
                    isBConnected = true;
            };

            try
            {
                using (var dialer = new AutoDialer(swarmA) { MinConnections = 1 })
                {
                    var b = await swarmA.RegisterPeerAsync(peerBAddress);
                    var c = await swarmA.RegisterPeerAsync(peerCAddress);

                    // wait for the peer B connection.
                    var endTime = DateTime.Now.AddSeconds(3);
                    while (!isBConnected)
                    {
                        if (DateTime.Now > endTime)
                            Assert.Fail("Did not do autodial on peer discovered");
                        await Task.Delay(100);
                    }
                    Assert.IsNull(c.ConnectedAddress);
                    await swarmA.DisconnectAsync(peerBAddress);

                    // wait for the peer C connection.
                    endTime = DateTime.Now.AddSeconds(3);
                    while (c.ConnectedAddress == null)
                    {
                        if (DateTime.Now > endTime)
                            Assert.Fail("Did not do autodial on peer disconnected");
                        await Task.Delay(100);
                    }
                }
            }
            finally
            {
                await swarmA?.StopAsync();
                await swarmB?.StopAsync();
                await swarmC?.StopAsync();
            }
        }

    }
}
