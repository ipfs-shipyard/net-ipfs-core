using System.Linq;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Http.Tests.CoreApi;

[TestClass]
public class BootstapApiTest
{
    IpfsClient _ipfs = TestFixture.Ipfs;
    MultiAddress _somewhere = "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

    [TestMethod]
    public async Task Add_Remove()
    {
        var addr = await _ipfs.Bootstrap.AddAsync(_somewhere);
        Assert.IsNotNull(addr);
        Assert.AreEqual<MultiAddress>(_somewhere, addr);
        var addrs = await _ipfs.Bootstrap.ListAsync();
        Assert.IsTrue(Enumerable.Any<MultiAddress>(addrs, a => a == _somewhere));

        addr = await _ipfs.Bootstrap.RemoveAsync(_somewhere);
        Assert.IsNotNull(addr);
        Assert.AreEqual<MultiAddress>(_somewhere, addr);
        addrs = await _ipfs.Bootstrap.ListAsync();
        Assert.IsFalse(Enumerable.Any<MultiAddress>(addrs, a => a == _somewhere));
    }

    [TestMethod]
    public async Task List()
    {
        var addrs = await _ipfs.Bootstrap.ListAsync();
        Assert.IsNotNull(addrs);
        Assert.AreNotEqual(0, Enumerable.Count<MultiAddress>(addrs));
    }

    [TestMethod]
    public async Task Remove_All()
    {
        var original = await _ipfs.Bootstrap.ListAsync();
        await _ipfs.Bootstrap.RemoveAllAsync();
        var addrs = await _ipfs.Bootstrap.ListAsync();
        Assert.AreEqual(0, Enumerable.Count<MultiAddress>(addrs));
        foreach (var addr in original)
        {
            await _ipfs.Bootstrap.AddAsync(addr);
        }
    }

    [TestMethod]
    public async Task Add_Defaults()
    {
        var original = await _ipfs.Bootstrap.ListAsync();
        await _ipfs.Bootstrap.RemoveAllAsync();
        try
        {
            await _ipfs.Bootstrap.AddDefaultsAsync();
            var addrs = await _ipfs.Bootstrap.ListAsync();
            Assert.AreNotEqual(0, Enumerable.Count<MultiAddress>(addrs));
        }
        finally
        {
            await _ipfs.Bootstrap.RemoveAllAsync();
            foreach (var addr in original)
            {
                await _ipfs.Bootstrap.AddAsync(addr);
            }
        }
    }
}