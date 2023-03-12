using System.Linq;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.PeerTalk.Tests.Protocols;

[TestClass]
public class PingTest
{
    private readonly Peer _self = new()
    {
        AgentVersion = "self",
        Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
        PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
    };

    private readonly Peer _other = new()
    {
        AgentVersion = "other",
        Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
        PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
    };


    [TestMethod]
    public async Task MultiAddress()
    {
        var swarmB = new Swarm { LocalPeer = _other };
        await swarmB.StartAsync();
        var pingB = new Ping1 { Swarm = swarmB };
        await pingB.StartAsync();
        var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

        var swarm = new Swarm { LocalPeer = _self };
        await swarm.StartAsync();
        var pingA = new Ping1 { Swarm = swarm };
        await pingA.StartAsync();
        try
        {
            await swarm.ConnectAsync(peerBAddress);
            var result = await pingA.PingAsync(_other.Id, 4);
            Assert.IsTrue(result.All(r => r.Success));
        }
        finally
        {
            await swarm.StopAsync();
            await swarmB.StopAsync();
            await pingB.StopAsync();
            await pingA.StopAsync();
        }
    }

    [TestMethod]
    public async Task PeerId()
    {
        var swarmB = new Swarm { LocalPeer = _other };
        await swarmB.StartAsync();
        var pingB = new Ping1 { Swarm = swarmB };
        await pingB.StartAsync();
        var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

        var swarm = new Swarm { LocalPeer = _self };
        await swarm.StartAsync();
        var pingA = new Ping1 { Swarm = swarm };
        await pingA.StartAsync();
        try
        {
            var result = await pingA.PingAsync(peerBAddress, 4);
            Assert.IsTrue(result.All(r => r.Success));
        }
        finally
        {
            await swarm.StopAsync();
            await swarmB.StopAsync();
            await pingB.StopAsync();
            await pingA.StopAsync();
        }
    }

}