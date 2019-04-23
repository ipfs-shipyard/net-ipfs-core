using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    
    [TestClass]
    public class Dht1Test
    {
        Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        Peer other = new Peer
        {
            AgentVersion = "other",
            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
            Addresses = new MultiAddress[]
            {
                new MultiAddress("/ip4/127.0.0.1/tcp/4001")
            }
        };

        [TestMethod]
        public async Task SeedsRoutingTableFromSwarm()
        {
            var swarm = new Swarm { LocalPeer = self };
            var peer = await swarm.RegisterPeerAsync("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
            var dht = new Dht1 { Swarm = swarm };
            await dht.StartAsync();
            try
            {
                Assert.IsTrue(dht.RoutingTable.Contains(peer));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [TestMethod]
        public async Task AddDiscoveredPeerToRoutingTable()
        {
            var swarm = new Swarm { LocalPeer = self };
            var dht = new Dht1 { Swarm = swarm };
            await dht.StartAsync();
            try
            {
                var peer = await swarm.RegisterPeerAsync("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
                Assert.IsTrue(dht.RoutingTable.Contains(peer));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [TestMethod]
        public async Task ProcessFindNodeMessage_Self()
        {
            var swarm = new Swarm { LocalPeer = self };
            var dht = new Dht1 { Swarm = swarm };
            await dht.StartAsync();
            try
            {
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = self.Id.ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.AreEqual(1, response.CloserPeers.Length);
                var ok = response.CloserPeers[0].TryToPeer(out Peer found);
                Assert.IsTrue(ok);
                Assert.AreEqual(self, found);
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [TestMethod]
        public async Task ProcessFindNodeMessage_InRoutingTable()
        {
            var swarm = new Swarm { LocalPeer = self };
            var dht = new Dht1 { Swarm = swarm };
            await dht.StartAsync();
            try
            {
                dht.RoutingTable.Add(other);
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = other.Id.ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.AreEqual(1, response.CloserPeers.Length);
                var ok = response.CloserPeers[0].TryToPeer(out Peer found);
                Assert.IsTrue(ok);
                Assert.AreEqual(other, found);
                CollectionAssert.AreEqual(other.Addresses.ToArray(), 
                    found.Addresses.Select(a => a.WithoutPeerId()).ToArray());
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [TestMethod]
        public async Task ProcessFindNodeMessage_InSwarm()
        {
            var swarm = new Swarm { LocalPeer = self };
            var other = await swarm.RegisterPeerAsync("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
            var dht = new Dht1 { Swarm = swarm };
            await dht.StartAsync();
            try
            {
                dht.RoutingTable.Add(other);
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = other.Id.ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.AreEqual(1, response.CloserPeers.Length);
                var ok = response.CloserPeers[0].TryToPeer(out Peer found);
                Assert.IsTrue(ok);
                Assert.AreEqual(other, found);
                CollectionAssert.AreEqual(
                    other.Addresses.Select(a => a.WithoutPeerId()).ToArray(),
                    found.Addresses.Select(a => a.WithoutPeerId()).ToArray());
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [TestMethod]
        public async Task ProcessFindNodeMessage_Closest()
        {
            var swarm = new Swarm { LocalPeer = self };
            await swarm.RegisterPeerAsync("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1a");
            await swarm.RegisterPeerAsync("/ip4/127.0.0.2/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1b");
            await swarm.RegisterPeerAsync("/ip4/127.0.0.3/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1c");
            await swarm.RegisterPeerAsync("/ip4/127.0.0.4/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1d");
            await swarm.RegisterPeerAsync("/ip4/127.0.0.5/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1e");
            var dht = new Dht1 { Swarm = swarm, CloserPeerCount = 3 };
            await dht.StartAsync();
            try
            {
                dht.RoutingTable.Add(other);
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = other.Id.ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.AreEqual(3, response.CloserPeers.Length);
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [TestMethod]
        public async Task ProcessFindNodeMessage_NoOtherPeers()
        {
            var swarm = new Swarm { LocalPeer = self };
            var dht = new Dht1 { Swarm = swarm };
            await dht.StartAsync();
            try
            {
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = new MultiHash("QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h").ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.AreEqual(0, response.CloserPeers.Length);
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [TestMethod]
        public async Task ProcessGetProvidersMessage_HasCloserPeers()
        {
            var swarm = new Swarm { LocalPeer = self };
            var dht = new Dht1 { Swarm = swarm };
            await dht.StartAsync();
            try
            {
                dht.RoutingTable.Add(other);
                Cid cid = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
                var request = new DhtMessage
                {
                    Type = MessageType.GetProviders,
                    Key = cid.Hash.ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.AreNotEqual(0, response.CloserPeers.Length);
            }
            finally
            {
                await dht.StopAsync();
            }
        }

    }
}
