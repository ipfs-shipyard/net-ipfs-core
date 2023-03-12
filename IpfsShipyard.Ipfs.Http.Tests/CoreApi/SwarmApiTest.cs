using System;
using System.Linq;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Http.Tests.CoreApi
{
    [TestClass]
    public class SwarmApiTest
    {
        [TestMethod]
        public async Task Addresses()
        {
            var ipfs = TestFixture.Ipfs;
            var swarm = await ipfs.Swarm.AddressesAsync();
            foreach (var peer in swarm)
            {
                Assert.IsNotNull(peer.Id);
                Assert.IsNotNull(peer.Addresses);
                Assert.AreNotEqual(0, Enumerable.Count<MultiAddress>(peer.Addresses));
            }
        }

        [TestMethod]
        public async Task Peers()
        {
            var ipfs = TestFixture.Ipfs;
            var peers = await ipfs.Swarm.PeersAsync();
            Assert.AreNotEqual(0, Enumerable.Count<Peer>(peers));
            foreach (var peer in peers)
            {
                Assert.IsNotNull(peer.Id);
                Assert.IsNotNull(peer.ConnectedAddress);
            }
        }

        [TestMethod]
        public async Task Peers_Info()
        {
            var ipfs = TestFixture.Ipfs;
            var peers = await ipfs.Swarm.PeersAsync();
            await Task.WhenAll(Enumerable.Where<Peer>(peers, p => p.Latency != TimeSpan.Zero)
                .OrderBy(p => p.Latency)
                .Take(1)
                .Select(async p =>
                {
                    var peer = await ipfs.IdAsync(p.Id);
                    Assert.AreNotEqual("", peer.PublicKey);
                }));
        }

        [TestMethod]
        public async Task Connection()
        {
            var ipfs = TestFixture.Ipfs;
            var peers = await ipfs.Swarm.PeersAsync();

            // Sometimes we cannot connect to a specific peer.  This
            // tests that a connection can be made to at least one peer.
            foreach (var peer in Enumerable.Take<Peer>(peers, 2))
            {
                try
                {
                    await ipfs.Swarm.ConnectAsync(peer.ConnectedAddress);
                    return;
                }
                catch (Exception)
                {
                    // eat it
                }
            }

            Assert.Fail("Cannot connect to any peer");
        }

        [TestMethod]
        public async Task Filter_Add_Remove()
        {
            var ipfs = TestFixture.Ipfs;
            var somewhere = new MultiAddress("/ip4/192.127.0.0/ipcidr/16");
            var filter = await ipfs.Swarm.AddAddressFilterAsync(somewhere, true);
            Assert.IsNotNull(filter);
            Assert.AreEqual<MultiAddress>(somewhere, filter);
            var filters = await ipfs.Swarm.ListAddressFiltersAsync();
            Assert.IsTrue(Enumerable.Any<MultiAddress>(filters, a => a == somewhere));
            filters = await ipfs.Swarm.ListAddressFiltersAsync(true);
            Assert.IsTrue(Enumerable.Any<MultiAddress>(filters, a => a == somewhere));

            filter = await ipfs.Swarm.RemoveAddressFilterAsync(somewhere, true);
            Assert.IsNotNull(filter);
            Assert.AreEqual<MultiAddress>(somewhere, filter);
            filters = await ipfs.Swarm.ListAddressFiltersAsync();
            Assert.IsFalse(Enumerable.Any<MultiAddress>(filters, a => a == somewhere));
            filters = await ipfs.Swarm.ListAddressFiltersAsync(true);
            Assert.IsFalse(Enumerable.Any<MultiAddress>(filters, a => a == somewhere));
        }

        [TestMethod]
        public async Task Filter_List()
        {
            var ipfs = TestFixture.Ipfs;
            var filters = await ipfs.Swarm.ListAddressFiltersAsync(persist: false);
            Assert.IsNotNull(filters);
        }

        [TestMethod]
        public async Task Filter_Remove_Unknown()
        {
            var ipfs = TestFixture.Ipfs;
            var somewhere = new MultiAddress("/ip4/192.168.0.3/ipcidr/2");

            var filter = await ipfs.Swarm.RemoveAddressFilterAsync(somewhere);
            Assert.IsNull(filter);
        }
    }
}
