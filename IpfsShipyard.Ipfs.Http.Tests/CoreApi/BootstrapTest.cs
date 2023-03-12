using System.Linq;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Http.Tests.CoreApi
{
    [TestClass]
    public class BootstapApiTest
    {
        Http.CoreApi.IpfsClient ipfs = TestFixture.Ipfs;
        MultiAddress somewhere = "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

        [TestMethod]
        public async Task Add_Remove()
        {
            var addr = await ipfs.Bootstrap.AddAsync(somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual<MultiAddress>(somewhere, addr);
            var addrs = await ipfs.Bootstrap.ListAsync();
            Assert.IsTrue(Enumerable.Any<MultiAddress>(addrs, a => a == somewhere));

            addr = await ipfs.Bootstrap.RemoveAsync(somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual<MultiAddress>(somewhere, addr);
            addrs = await ipfs.Bootstrap.ListAsync();
            Assert.IsFalse(Enumerable.Any<MultiAddress>(addrs, a => a == somewhere));
        }

        [TestMethod]
        public async Task List()
        {
            var addrs = await ipfs.Bootstrap.ListAsync();
            Assert.IsNotNull(addrs);
            Assert.AreNotEqual(0, Enumerable.Count<MultiAddress>(addrs));
        }

        [TestMethod]
        public async Task Remove_All()
        {
            var original = await ipfs.Bootstrap.ListAsync();
            await ipfs.Bootstrap.RemoveAllAsync();
            var addrs = await ipfs.Bootstrap.ListAsync();
            Assert.AreEqual(0, Enumerable.Count<MultiAddress>(addrs));
            foreach (var addr in original)
            {
                await ipfs.Bootstrap.AddAsync(addr);
            }
        }

        [TestMethod]
        public async Task Add_Defaults()
        {
            var original = await ipfs.Bootstrap.ListAsync();
            await ipfs.Bootstrap.RemoveAllAsync();
            try
            {
                await ipfs.Bootstrap.AddDefaultsAsync();
                var addrs = await ipfs.Bootstrap.ListAsync();
                Assert.AreNotEqual(0, Enumerable.Count<MultiAddress>(addrs));
            }
            finally
            {
                await ipfs.Bootstrap.RemoveAllAsync();
                foreach (var addr in original)
                {
                    await ipfs.Bootstrap.AddAsync(addr);
                }
            }
        }
    }
}
