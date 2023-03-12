﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class ObjectApiTest
{
    private readonly IpfsEngine _ipfs = TestFixture.Ipfs;

    [TestMethod]
    public async Task New_Template_Null()
    {
        var node = await _ipfs.Object.NewAsync();
        Assert.AreEqual("QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1n", (string)node.Id);
    }

    [TestMethod]
    public async Task New_Template_UnixfsDir()
    {
        var node = await _ipfs.Object.NewAsync("unixfs-dir");
        Assert.AreEqual("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn", (string)node.Id);

        node = await _ipfs.Object.NewDirectoryAsync();
        Assert.AreEqual("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn", (string)node.Id);
    }

    [TestMethod]
    public void New_Template_Unknown()
    {
        ExceptionAssert.Throws<Exception>(() =>
        {
            var node = _ipfs.Object.NewAsync("unknown-template").Result;
        });
    }

    [TestMethod]
    public async Task Put_Get_Dag()
    {
        var adata = "alpha"u8.ToArray();
        var bdata = "beta"u8.ToArray();
        var alpha = new DagNode(adata);
        var beta = new DagNode(bdata, new[] { alpha.ToLink() });
        var x = await _ipfs.Object.PutAsync(beta);
        var node = await _ipfs.Object.GetAsync(x.Id);
        CollectionAssert.AreEqual(beta.DataBytes, node.DataBytes);
        Assert.AreEqual(beta.Links.Count(), node.Links.Count());
        Assert.AreEqual(beta.Links.First().Id, node.Links.First().Id);
        Assert.AreEqual(beta.Links.First().Name, node.Links.First().Name);
        Assert.AreEqual(beta.Links.First().Size, node.Links.First().Size);
    }

    [TestMethod]
    public async Task Put_Get_Data()
    {
        var adata = "alpha"u8.ToArray();
        var bdata = "beta"u8.ToArray();
        var alpha = new DagNode(adata);
        var beta = await _ipfs.Object.PutAsync(bdata, new[] { alpha.ToLink() });
        var node = await _ipfs.Object.GetAsync(beta.Id);
        CollectionAssert.AreEqual(beta.DataBytes, node.DataBytes);
        Assert.AreEqual(beta.Links.Count(), node.Links.Count());
        Assert.AreEqual(beta.Links.First().Id, node.Links.First().Id);
        Assert.AreEqual(beta.Links.First().Name, node.Links.First().Name);
        Assert.AreEqual(beta.Links.First().Size, node.Links.First().Size);
    }

    [TestMethod]
    public async Task Data()
    {
        var adata = "alpha"u8.ToArray();
        var node = await _ipfs.Object.PutAsync(adata);
        await using var stream = await _ipfs.Object.DataAsync(node.Id);
        var bdata = new byte[adata.Length];
        await stream.ReadAsync(bdata, 0, bdata.Length);
        CollectionAssert.AreEqual(adata, bdata);
    }

    [TestMethod]
    public async Task Links()
    {
        var adata = "alpha"u8.ToArray();
        var bdata = "beta"u8.ToArray();
        var alpha = new DagNode(adata);
        var beta = await _ipfs.Object.PutAsync(bdata, new[] { alpha.ToLink() });
        var links = await _ipfs.Object.LinksAsync(beta.Id);
        var merkleLinks = links.ToList();
        Assert.AreEqual(beta.Links.Count(), merkleLinks.Count);
        Assert.AreEqual(beta.Links.First().Id, merkleLinks.First().Id);
        Assert.AreEqual(beta.Links.First().Name, merkleLinks.First().Name);
        Assert.AreEqual(beta.Links.First().Size, merkleLinks.First().Size);
    }

    [TestMethod]
    public async Task Stat()
    {
        var data1 = "Some data 1"u8.ToArray();
        var data2 = "Some data 2"u8.ToArray();
        var node2 = new DagNode(data2);
        var node1 = await _ipfs.Object.PutAsync(data1,
            new[] { node2.ToLink("some-link") });
        var info = await _ipfs.Object.StatAsync(node1.Id);
        Assert.AreEqual(1, info.LinkCount);
        Assert.AreEqual(11, info.DataSize);
        Assert.AreEqual(64, info.BlockSize);
        Assert.AreEqual(53, info.LinkSize);
        Assert.AreEqual(77, info.CumulativeSize);
    }

    [TestMethod]
    public async Task Get_Nonexistent()
    {
        var data = "Some data for net-ipfs-engine-test that cannot be found"u8.ToArray();
        var node = new DagNode(data);
        var id = node.Id;
        var cs = new CancellationTokenSource(500);
        try
        {
            var _ = await _ipfs.Object.GetAsync(id, cs.Token);
            Assert.Fail("Did not throw TaskCanceledException");
        }
        catch (TaskCanceledException)
        {
        }
    }

    [TestMethod]
    /// <seealso href="https://github.com/ipfs/js-ipfs/issues/2084"/>
    public async Task Get_Inlinefile()
    {
        var original = _ipfs.Options.Block.AllowInlineCid;
        try
        {
            _ipfs.Options.Block.AllowInlineCid = true;

            var node = await _ipfs.FileSystem.AddTextAsync("hiya");
            Assert.AreEqual(1, node.Id.Version);
            Assert.IsTrue(node.Id.Hash.IsIdentityHash);

            var dag = await _ipfs.Object.GetAsync(node.Id);
            Assert.AreEqual(12, dag.Size);
        }
        finally
        {
            _ipfs.Options.Block.AllowInlineCid = original;
        }
    }

    [TestMethod]
    public async Task Links_InlineCid()
    {
        var original = _ipfs.Options.Block.AllowInlineCid;
        try
        {
            _ipfs.Options.Block.AllowInlineCid = true;

            var node = await _ipfs.FileSystem.AddTextAsync("hiya");
            Assert.AreEqual(1, node.Id.Version);
            Assert.IsTrue(node.Id.Hash.IsIdentityHash);

            var links = await _ipfs.Object.LinksAsync(node.Id);
            Assert.AreEqual(0, links.Count());
        }
        finally
        {
            _ipfs.Options.Block.AllowInlineCid = original;
        }
    }

    [TestMethod]
    public async Task Links_RawCid()
    {
        var blob = new byte[2048];
        var cid = await _ipfs.Block.PutAsync(blob, "raw");

        var links = await _ipfs.Object.LinksAsync(cid);
        Assert.AreEqual(0, links.Count());
    }
}