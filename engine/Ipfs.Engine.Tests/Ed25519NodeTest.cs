using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Ipfs.Engine.Tests;

[TestClass]
public class Ed25519NodeTest
{
    [TestMethod]
    public async Task Can_Create()
    {
        var ed = await CreateNode();
        try
        {
            Assert.IsNotNull(ed);
            var node = await ed.LocalPeer;
            Assert.IsNotNull(node);
        }
        finally
        {
            DeleteNode(ed);
        }
    }

    [TestMethod]
    public async Task CanConnect()
    {
        var ed = await CreateNode();
        try
        {
            await ed.StartAsync();
            var node = await ed.LocalPeer;
            Assert.AreNotEqual(0, node.Addresses.Count());
            var addr = node.Addresses.First();

            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            try
            {
                await ipfs.Swarm.ConnectAsync(addr);
                var peers = await ipfs.Swarm.PeersAsync();
                Assert.IsTrue(peers.Any(p => p.Id == addr.PeerId));
                await ipfs.Swarm.DisconnectAsync(addr);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }
        finally
        {
            await ed.StopAsync();
            DeleteNode(ed);
        }
    }

    private static async Task<IpfsEngine> CreateNode()
    {
        const string passphrase = "this is not a secure pass phrase";
        var ipfs = new IpfsEngine(passphrase.ToCharArray());
        ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-ed255129-test");
        ipfs.Options.KeyChain.DefaultKeyType = "ed25519";
        await ipfs.Config.SetAsync(
            "Addresses.Swarm",
            JToken.FromObject(new[] { "/ip4/0.0.0.0/tcp/0" })
        );
        return ipfs;
    }

    private static void DeleteNode(IpfsEngine ipfs)
    {
        if (Directory.Exists(ipfs.Options.Repository.Folder))
        {
            Directory.Delete(ipfs.Options.Repository.Folder, true);
        }
    }
}