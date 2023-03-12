using System;
using System.Linq;
using System.Threading;
using Ipfs.Engine.BlockExchange;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests.BlockExchange;

[TestClass]
public class BitswapTest
{
    private readonly Peer _self = new()
    {
        Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
        PublicKey =
            "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
    };

    [TestMethod]
    public void WantList()
    {
        var bitswap = new Bitswap { Swarm = new() { LocalPeer = _self } };
        Assert.AreEqual(0, bitswap.PeerWants(_self.Id).Count());

        var cid = new DagNode("BitswapTest unknown block"u8.ToArray()).Id;
        var cancel = new CancellationTokenSource();
        var task = bitswap.WantAsync(cid, _self.Id, cancel.Token);
        CollectionAssert.Contains(bitswap.PeerWants(_self.Id).ToArray(), cid);

        bitswap.Unwant(cid);
        CollectionAssert.DoesNotContain(bitswap.PeerWants(_self.Id).ToArray(), cid);
    }

    [TestMethod]
    public void Want_Cancel()
    {
        var bitswap = new Bitswap { Swarm = new() { LocalPeer = _self } };
        var cid = new DagNode("BitswapTest unknown block"u8.ToArray()).Id;
        var cancel = new CancellationTokenSource();
        var task = bitswap.WantAsync(cid, _self.Id, cancel.Token);
        CollectionAssert.Contains(bitswap.PeerWants(_self.Id).ToArray(), cid);

        cancel.Cancel();
        Assert.IsTrue(task.IsCanceled);
        CollectionAssert.DoesNotContain(bitswap.PeerWants(_self.Id).ToArray(), cid);
    }

    [TestMethod]
    public void Block_Needed()
    {
        var bitswap = new Bitswap { Swarm = new() { LocalPeer = _self } };
        var cid1 = new DagNode("BitswapTest unknown block y"u8.ToArray()).Id;
        var cid2 = new DagNode("BitswapTest unknown block z"u8.ToArray()).Id;
        var cancel = new CancellationTokenSource();
        var callCount = 0;
        bitswap.BlockNeeded += (s, e) =>
        {
            ArgumentNullException.ThrowIfNull(s);
            ArgumentNullException.ThrowIfNull(e);
            Assert.IsTrue(cid1 == e.Id || cid2 == e.Id);
            ++callCount;
        };
        try
        {
            bitswap.WantAsync(cid1, _self.Id, cancel.Token);
            bitswap.WantAsync(cid1, _self.Id, cancel.Token);
            bitswap.WantAsync(cid2, _self.Id, cancel.Token);
            bitswap.WantAsync(cid2, _self.Id, cancel.Token);
            Assert.AreEqual(2, callCount);
        }
        finally
        {
            cancel.Cancel();
        }
    }

    [TestMethod]
    public void Want_Unwant()
    {
        var bitswap = new Bitswap { Swarm = new() { LocalPeer = _self } };
        var cid = new DagNode("BitswapTest unknown block"u8.ToArray()).Id;
        var cancel = new CancellationTokenSource();
        var task = bitswap.WantAsync(cid, _self.Id, cancel.Token);
        CollectionAssert.Contains(bitswap.PeerWants(_self.Id).ToArray(), cid);

        bitswap.Unwant(cid);
        Assert.IsTrue(task.IsCanceled);
        CollectionAssert.DoesNotContain(bitswap.PeerWants(_self.Id).ToArray(), cid);
    }

    [TestMethod]
    public void Found()
    {
        var bitswap = new Bitswap { Swarm = new() { LocalPeer = _self } };
        Assert.AreEqual(0, bitswap.PeerWants(_self.Id).Count());

        var a = new DagNode("BitswapTest found block a"u8.ToArray());
        var b = new DagNode("BitswapTest found block b"u8.ToArray());
        var cancel = new CancellationTokenSource();
        var task = bitswap.WantAsync(a.Id, _self.Id, cancel.Token);
        Assert.IsFalse(task.IsCompleted);
        CollectionAssert.Contains(bitswap.PeerWants(_self.Id).ToArray(), a.Id);

        bitswap.Found(b);
        Assert.IsFalse(task.IsCompleted);
        CollectionAssert.Contains(bitswap.PeerWants(_self.Id).ToArray(), a.Id);

        bitswap.Found(a);
        Assert.IsTrue(task.IsCompleted);
        CollectionAssert.DoesNotContain(bitswap.PeerWants(_self.Id).ToArray(), a.Id);
        CollectionAssert.AreEqual(a.DataBytes, task.Result.DataBytes);
    }

    [TestMethod]
    public void Found_Count()
    {
        var bitswap = new Bitswap { Swarm = new() { LocalPeer = _self } };

        var a = new DagNode("BitswapTest found block a"u8.ToArray());
        Assert.AreEqual(0, bitswap.Found(a));

        var cancel = new CancellationTokenSource();
        var task1 = bitswap.WantAsync(a.Id, _self.Id, cancel.Token);
        var task2 = bitswap.WantAsync(a.Id, _self.Id, cancel.Token);
        Assert.AreEqual(2, bitswap.Found(a));

        Assert.IsTrue(task1.IsCompleted);
        Assert.IsTrue(task2.IsCompleted);
    }
}