using System.IO;
using Common.Logging;
using Common.Logging.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Engine.Tests;

[TestClass]
public class TestFixture
{
    private const string Passphrase = "this is not a secure pass phrase";
    public static IpfsEngine Ipfs = new(Passphrase);
    public static IpfsEngine IpfsOther = new(Passphrase);

    static TestFixture()
    {
        Ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-test");
        Ipfs.Options.KeyChain.DefaultKeySize = 512;
        Ipfs.Config.SetAsync(
            "Addresses.Swarm",
            JToken.FromObject(new[] { "/ip4/0.0.0.0/tcp/0" })
        ).Wait();

        IpfsOther.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-other");
        IpfsOther.Options.KeyChain.DefaultKeySize = 512;
        IpfsOther.Config.SetAsync(
            "Addresses.Swarm",
            JToken.FromObject(new[] { "/ip4/0.0.0.0/tcp/0" })
        ).Wait();
    }

    [TestMethod]
    public void Engine_Exists()
    {
        Assert.IsNotNull(Ipfs);
        Assert.IsNotNull(IpfsOther);
    }

    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        // set logger factory
        var properties = new NameValueCollection
        {
            ["level"] = "DEBUG",
            ["showLogName"] = "true",
            ["showDateTime"] = "true",
            ["dateTimeFormat"] = "HH:mm:ss.fff"
        };
        LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);
    }

    [AssemblyCleanup]
    public static void Cleanup()
    {
        if (Directory.Exists(Ipfs.Options.Repository.Folder))
        {
            Directory.Delete(Ipfs.Options.Repository.Folder, true);
        }

        if (Directory.Exists(IpfsOther.Options.Repository.Folder))
        {
            Directory.Delete(IpfsOther.Options.Repository.Folder, true);
        }
    }
}