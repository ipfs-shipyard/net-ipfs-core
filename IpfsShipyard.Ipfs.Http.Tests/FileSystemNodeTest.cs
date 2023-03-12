using System.Linq;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace IpfsShipyard.Ipfs.Http.Tests
{
    [TestClass]
    public class FileSystemNodeTest
    {
        [TestMethod]
        public async Task Serialization()
        {
            var ipfs = TestFixture.Ipfs;
            var a = await ipfs.FileSystem.AddTextAsync("hello world");
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)a.Id);

            var b = await ipfs.FileSystem.ListFileAsync(a.Id);
            var json = JsonConvert.SerializeObject(b);
            var c = JsonConvert.DeserializeObject<FileSystemNode>(json);
            Assert.AreEqual<Cid>(b.Id, c.Id);
            Assert.AreEqual<bool>(b.IsDirectory, c.IsDirectory);
            Assert.AreEqual<long>(b.Size, c.Size);
            CollectionAssert.AreEqual(Enumerable.ToArray<IFileSystemLink>(b.Links), c.Links.ToArray());
        }
    }
}
