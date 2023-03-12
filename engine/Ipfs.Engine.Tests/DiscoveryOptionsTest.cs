using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests;

[TestClass]
public class DiscoveryOptionsTest
{
    [TestMethod]
    public void Defaults()
    {
        var options = new DiscoveryOptions();
        Assert.IsNull(options.BootstrapPeers);
        Assert.IsFalse(options.DisableMdns);
    }
}