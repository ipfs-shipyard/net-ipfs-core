using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class PinApiTest
{
    [TestMethod]
    public async Task Add_Remove()
    {
        var ipfs = TestFixture.Ipfs;
        var result = await ipfs.FileSystem.AddTextAsync("I am pinned");
        var id = result.Id;

        var pins = await ipfs.Pin.AddAsync(id);
        Assert.IsTrue(pins.Any(pin => pin == id));
        var all = await ipfs.Pin.ListAsync();
        Assert.IsTrue(all.Any(pin => pin == id));

        pins = await ipfs.Pin.RemoveAsync(id);
        Assert.IsTrue(pins.Any(pin => pin == id));
        all = await ipfs.Pin.ListAsync();
        Assert.IsFalse(all.Any(pin => pin == id));
    }

    [TestMethod]
    public async Task Remove_Unknown()
    {
        var ipfs = TestFixture.Ipfs;
        var dag = new DagNode("some unknown info for net-ipfs-engine-pin-test"u8.ToArray());
        await ipfs.Pin.RemoveAsync(dag.Id);
    }

    [TestMethod]
    public async Task Inline_Cid()
    {
        var ipfs = TestFixture.Ipfs;
        var cid = new Cid
        {
            ContentType = "raw",
            Hash = MultiHash.ComputeHash(new byte[] { 1, 2, 3 }, "identity")
        };
        var pins = await ipfs.Pin.AddAsync(cid, false);
        CollectionAssert.Contains(pins.ToArray(), cid);
        var all = await ipfs.Pin.ListAsync();
        CollectionAssert.Contains(all.ToArray(), cid);

        var removals = await ipfs.Pin.RemoveAsync(cid, false);
        CollectionAssert.Contains(removals.ToArray(), cid);
        all = await ipfs.Pin.ListAsync();
        CollectionAssert.DoesNotContain(all.ToArray(), cid);
    }

    [TestMethod]
    public void Add_Unknown()
    {
        var ipfs = TestFixture.Ipfs;
        var dag = new DagNode("some unknown info for net-ipfs-engine-pin-test"u8.ToArray());
        ExceptionAssert.Throws<Exception>(() =>
        {
            var cts = new CancellationTokenSource(250);
            var _ = ipfs.Pin.AddAsync(dag.Id, true, cts.Token).Result;
        });
    }

    [TestMethod]
    public async Task Add_Recursive()
    {
        var ipfs = TestFixture.Ipfs;
        var options = new AddFileOptions
        {
            ChunkSize = 3,
            Pin = false,
            RawLeaves = true,
            Wrap = true
        };
        var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
        var cids = await ipfs.Pin.AddAsync(node.Id);
        Assert.AreEqual(6, cids.Count());
    }

    [TestMethod]
    public async Task Remove_Recursive()
    {
        var ipfs = TestFixture.Ipfs;
        var options = new AddFileOptions
        {
            ChunkSize = 3,
            Pin = false,
            RawLeaves = true,
            Wrap = true
        };
        var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
        var cids = await ipfs.Pin.AddAsync(node.Id);
        var enumerable = cids.ToList();
        Assert.AreEqual(6, enumerable.Count);

        var removedCids = await ipfs.Pin.RemoveAsync(node.Id);
        CollectionAssert.AreEqual(enumerable.ToArray(), removedCids.ToArray());
    }
}