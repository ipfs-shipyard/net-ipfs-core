using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class NameApiTest
{
    private readonly IpfsEngine _ipfs = TestFixture.Ipfs;

    [TestMethod]
    public async Task Resolve_DnsLink()
    {
        var iopath = await _ipfs.Name.ResolveAsync("ipfs.io");
        Assert.IsNotNull(iopath);

        var path = await _ipfs.Name.ResolveAsync("/ipns/ipfs.io");
        Assert.AreEqual(iopath, path);
    }

    [TestMethod]
    public async Task Resolve_DnsLink_Recursive()
    {
        var path = await _ipfs.Name.ResolveAsync("/ipns/ipfs.io/media", true);
        StringAssert.StartsWith(path, "/ipfs/");
        StringAssert.EndsWith(path, "/media");

        path = await _ipfs.Name.ResolveAsync("ipfs.io/media", true);
        StringAssert.StartsWith(path, "/ipfs/");
        StringAssert.EndsWith(path, "/media");

        path = await _ipfs.Name.ResolveAsync("/ipfs.io/media", true);
        StringAssert.StartsWith(path, "/ipfs/");
        StringAssert.EndsWith(path, "/media");
    }

    [TestMethod]
    public void Resolve_NoDnsLink()
    {
        ExceptionAssert.Throws<Exception>(() =>
        {
            var _ = _ipfs.Dns.ResolveAsync("google.com").Result;
        });
    }
}