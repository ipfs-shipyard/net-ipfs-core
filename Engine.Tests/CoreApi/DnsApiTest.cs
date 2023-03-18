using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class DnsApiTest
{
    private readonly IpfsEngine _ipfs = TestFixture.Ipfs;

    [TestMethod]
    public async Task Resolve()
    {
        var path = await _ipfs.Dns.ResolveAsync("ipfs.io");
        Assert.IsNotNull(path);
    }

    [TestMethod]
    public void Resolve_NoLink()
    {
        ExceptionAssert.Throws<Exception>(() =>
        {
            var _ = _ipfs.Dns.ResolveAsync("google.com").Result;
        });
    }

    [TestMethod]
    public async Task Resolve_Recursive()
    {
        var path = await _ipfs.Dns.ResolveAsync("ipfs.io", true);
        StringAssert.StartsWith(path, "/ipfs/");
    }
}