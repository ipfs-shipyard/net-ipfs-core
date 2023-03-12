using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class BootstapApiTest
{
    private readonly IpfsEngine _ipfs = TestFixture.Ipfs;

    private readonly MultiAddress _somewhere =
        "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

    [TestMethod]
    public async Task Add_Remove()
    {
        var addr = await _ipfs.Bootstrap.AddAsync(_somewhere);
        Assert.IsNotNull(addr);
        Assert.AreEqual(_somewhere, addr);
        var addrs = await _ipfs.Bootstrap.ListAsync();
        Assert.IsTrue(addrs.Any(a => a == _somewhere));

        addr = await _ipfs.Bootstrap.RemoveAsync(_somewhere);
        Assert.IsNotNull(addr);
        Assert.AreEqual(_somewhere, addr);
        addrs = await _ipfs.Bootstrap.ListAsync();
        Assert.IsFalse(addrs.Any(a => a == _somewhere));
    }

    [TestMethod]
    public async Task List()
    {
        var addrs = await _ipfs.Bootstrap.ListAsync();
        Assert.IsNotNull(addrs);
        Assert.AreNotEqual(0, addrs.Count());
    }

    [TestMethod]
    public async Task Remove_All()
    {
        var original = await _ipfs.Bootstrap.ListAsync();
        await _ipfs.Bootstrap.RemoveAllAsync();
        var addrs = await _ipfs.Bootstrap.ListAsync();
        Assert.AreEqual(0, addrs.Count());
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
            Assert.AreNotEqual(0, addrs.Count());
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

    [TestMethod]
    public async Task Override_FactoryDefaults()
    {
        var original = _ipfs.Options.Discovery.BootstrapPeers;
        try
        {
            _ipfs.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
            var addrs = await _ipfs.Bootstrap.ListAsync();
            Assert.AreEqual(0, addrs.Count());

            _ipfs.Options.Discovery.BootstrapPeers = new[]
                { _somewhere };
            addrs = await _ipfs.Bootstrap.ListAsync();
            var multiAddresses = addrs.ToList();
            Assert.AreEqual(1, multiAddresses.Count);
            Assert.AreEqual(_somewhere, multiAddresses.First());
        }
        finally
        {
            _ipfs.Options.Discovery.BootstrapPeers = original;
        }
    }
}