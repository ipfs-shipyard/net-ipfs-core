using System.Linq;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Http.Tests.CoreApi
{
    [TestClass]
    public class PinApiTest
    {
        [TestMethod]
        public void List()
        {
            var ipfs = TestFixture.Ipfs;
            var pins = ipfs.Pin.ListAsync().Result;
            Assert.IsNotNull(pins);
            Assert.IsTrue(Enumerable.Count<Cid>(pins) > 0);
        }

        [TestMethod]
        public async Task Add_Remove()
        {
            var ipfs = TestFixture.Ipfs;
            var result = await ipfs.FileSystem.AddTextAsync("I am pinned");
            var id = result.Id;

            var pins = await ipfs.Pin.AddAsync(id);
            Assert.IsTrue(Enumerable.Any<Cid>(pins, pin => pin == id));
            var all = await ipfs.Pin.ListAsync();
            Assert.IsTrue(Enumerable.Any<Cid>(all, pin => pin == id));

            pins = await ipfs.Pin.RemoveAsync(id);
            Assert.IsTrue(Enumerable.Any<Cid>(pins, pin => pin == id));
            all = await ipfs.Pin.ListAsync();
            Assert.IsFalse(Enumerable.Any<Cid>(all, pin => pin == id));
        }

    }
}
