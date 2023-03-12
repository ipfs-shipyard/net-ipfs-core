using System;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.PeerTalk.Tests;

[TestClass]
public class PeerManagerTest
{
    readonly Peer _self = new Peer
    {
        AgentVersion = "self",
        Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
        PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
    };

    [TestMethod]
    public void IsNotReachable()
    {
        var peer = new Peer { Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb" };
        var manager = new PeerManager { Swarm = new Swarm() };
        Assert.AreEqual(0, manager.DeadPeers.Count);

        manager.SetNotReachable(peer);
        Assert.IsTrue(manager.DeadPeers.ContainsKey(peer));
        Assert.AreEqual(1, manager.DeadPeers.Count);

        manager.SetNotReachable(peer);
        Assert.IsTrue(manager.DeadPeers.ContainsKey(peer));
        Assert.AreEqual(1, manager.DeadPeers.Count);

        manager.SetReachable(peer);
        Assert.IsFalse(manager.DeadPeers.ContainsKey(peer));
        Assert.AreEqual(0, manager.DeadPeers.Count);
    }

    [TestMethod]
    public void BlackListsThePeer()
    {
        var peer = new Peer { Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb" };
        var manager = new PeerManager { Swarm = new Swarm() };
        Assert.AreEqual(0, manager.DeadPeers.Count);

        manager.SetNotReachable(peer);
        Assert.IsFalse(manager.Swarm.IsAllowed((MultiAddress)"/p2p/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"));

        manager.SetReachable(peer);
        Assert.IsTrue(manager.Swarm.IsAllowed((MultiAddress)"/p2p/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"));
    }

    [TestMethod]
    public async Task Backoff_Increases()
    {
        var peer = new Peer
        {
            Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTxx",
            Addresses = new MultiAddress[]
            {
                "/ip4/127.0.0.1/tcp/4040/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTxx"
            }
        };
        var swarm = new Swarm { LocalPeer = _self };
        var manager = new PeerManager
        {
            Swarm = swarm,
            InitialBackoff = TimeSpan.FromMilliseconds(100),
        };
        Assert.AreEqual(0, manager.DeadPeers.Count);

        try
        {
            await swarm.StartAsync();
            await manager.StartAsync();
            try
            {
                await swarm.ConnectAsync(peer);
            }
            catch { }
            Assert.AreEqual(1, manager.DeadPeers.Count);
                
            var end = DateTime.Now + TimeSpan.FromSeconds(4);
            while (DateTime.Now <= end)
            {
                if (manager.DeadPeers[peer].Backoff > manager.InitialBackoff)
                    return;
            }
            Assert.Fail("backoff did not increase");
        }
        finally
        {
            await swarm.StopAsync();
            await manager.StopAsync();
        }
    }

    [TestMethod]
    public async Task PermanentlyDead()
    {
        var peer = new Peer
        {
            Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
            Addresses = new MultiAddress[]
            {
                "/ip4/127.0.0.1/tcp/4040/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"
            }
        };
        var swarm = new Swarm { LocalPeer = _self };
        var manager = new PeerManager
        {
            Swarm = swarm,
            InitialBackoff = TimeSpan.FromMilliseconds(100),
            MaxBackoff = TimeSpan.FromMilliseconds(200),
        };
        Assert.AreEqual(0, manager.DeadPeers.Count);

        try
        {
            await swarm.StartAsync();
            await manager.StartAsync();
            try
            {
                await swarm.ConnectAsync(peer);
            }
            catch { }
            Assert.AreEqual(1, manager.DeadPeers.Count);

            var end = DateTime.Now + TimeSpan.FromSeconds(6);
            while (DateTime.Now <= end)
            {
                if (manager.DeadPeers[peer].NextAttempt == DateTime.MaxValue)
                    return;
            }
            Assert.Fail("not truely dead");
        }
        finally
        {
            await swarm.StopAsync();
            await manager.StopAsync();
        }
    }

}