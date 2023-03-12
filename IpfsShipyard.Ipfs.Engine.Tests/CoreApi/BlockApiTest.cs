using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class BlockApiTest
{
    private readonly byte[] _blob = "blorb"u8.ToArray();
    private readonly string _id = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";
    private readonly IpfsEngine _ipfs = TestFixture.Ipfs;

    [TestMethod]
    public void Put_Bytes()
    {
        var cid = _ipfs.Block.PutAsync(_blob).Result;
        Assert.AreEqual(_id, (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Bytes_TooBig()
    {
        var data = new byte[_ipfs.Options.Block.MaxBlockSize + 1];
        ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var cid = _ipfs.Block.PutAsync(data).Result;
        });
    }

    [TestMethod]
    public void Put_Bytes_ContentType()
    {
        var cid = _ipfs.Block.PutAsync(_blob, "raw").Result;
        Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Bytes_Inline_Cid()
    {
        try
        {
            _ipfs.Options.Block.AllowInlineCid = true;
            var cid = _ipfs.Block.PutAsync(_blob, "raw").Result;
            Assert.IsTrue(cid.Hash.IsIdentityHash);
            Assert.AreEqual("bafkqablcnrxxeyq", (string)cid);

            var data = _ipfs.Block.GetAsync(cid).Result;
            Assert.AreEqual(_blob.Length, data.Size);
            CollectionAssert.AreEqual(_blob, data.DataBytes);

            var content = new byte[_ipfs.Options.Block.InlineCidLimit];
            cid = _ipfs.Block.PutAsync(content, "raw").Result;
            Assert.IsTrue(cid.Hash.IsIdentityHash);

            content = new byte[_ipfs.Options.Block.InlineCidLimit + 1];
            cid = _ipfs.Block.PutAsync(content, "raw").Result;
            Assert.IsFalse(cid.Hash.IsIdentityHash);
        }
        finally
        {
            _ipfs.Options.Block.AllowInlineCid = false;
        }
    }

    [TestMethod]
    public void Put_Bytes_Hash()
    {
        var cid = _ipfs.Block.PutAsync(_blob, "raw", "sha2-512").Result;
        Assert.AreEqual(
            "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
            (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Bytes_Cid_Encoding()
    {
        var cid = _ipfs.Block.PutAsync(_blob,
            "raw",
            encoding: "base32").Result;
        Assert.AreEqual(1, cid.Version);
        Assert.AreEqual("base32", cid.Encoding);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Stream()
    {
        var cid = _ipfs.Block.PutAsync(new MemoryStream(_blob)).Result;
        Assert.AreEqual(_id, (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Stream_ContentType()
    {
        var cid = _ipfs.Block.PutAsync(new MemoryStream(_blob), "raw").Result;
        Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Stream_Hash()
    {
        var cid = _ipfs.Block.PutAsync(new MemoryStream(_blob), "raw", "sha2-512").Result;
        Assert.AreEqual(
            "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
            (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Get()
    {
        var _ = _ipfs.Block.PutAsync(_blob).Result;
        var block = _ipfs.Block.GetAsync(_id).Result;
        Assert.AreEqual(_id, (string)block.Id);
        CollectionAssert.AreEqual(_blob, block.DataBytes);
        var blob1 = new byte[_blob.Length];
        block.DataStream.Read(blob1, 0, blob1.Length);
        CollectionAssert.AreEqual(_blob, blob1);
    }

    [TestMethod]
    public void Stat()
    {
        var _ = _ipfs.Block.PutAsync(_blob).Result;
        var info = _ipfs.Block.StatAsync(_id).Result;
        Assert.AreEqual(_id, (string)info.Id);
        Assert.AreEqual(5, info.Size);
    }

    [TestMethod]
    public async Task Stat_Inline_CID()
    {
        var cts = new CancellationTokenSource(300);
        var cid = new Cid
        {
            ContentType = "raw",
            Hash = MultiHash.ComputeHash(_blob, "identity")
        };
        var info = await _ipfs.Block.StatAsync(cid, cts.Token);
        Assert.AreEqual(cid.Encode(), (string)info.Id);
        Assert.AreEqual(5, info.Size);
    }

    [TestMethod]
    public async Task Stat_Unknown()
    {
        var cid = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF";
        var block = await _ipfs.Block.StatAsync(cid);
        Assert.IsNull(block, "block should not exist locally");
    }

    [TestMethod]
    public async Task Remove()
    {
        var _ = _ipfs.Block.PutAsync(_blob).Result;
        var cid = await _ipfs.Block.RemoveAsync(_id);
        Assert.AreEqual(_id, (string)cid);
    }

    [TestMethod]
    public async Task Remove_Inline_CID()
    {
        var cid = new Cid
        {
            ContentType = "raw",
            Hash = MultiHash.ComputeHash(_blob, "identity")
        };
        var removedCid = await _ipfs.Block.RemoveAsync(cid);
        Assert.AreEqual(cid.Encode(), removedCid.Encode());
    }

    [TestMethod]
    public void Remove_Unknown()
    {
        ExceptionAssert.Throws<Exception>(() =>
        {
            var _ = _ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF").Result;
        });
    }

    [TestMethod]
    public async Task Remove_Unknown_OK()
    {
        var cid = await _ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF", true);
        Assert.AreEqual(null, cid);
    }

    [TestMethod]
    public async Task Get_Inline_CID()
    {
        var cts = new CancellationTokenSource(300);
        var cid = new Cid
        {
            ContentType = "raw",
            Hash = MultiHash.ComputeHash(_blob, "identity")
        };
        var block = await _ipfs.Block.GetAsync(cid, cts.Token);
        Assert.AreEqual(cid.Encode(), block.Id.Encode());
        Assert.AreEqual(_blob.Length, block.Size);
        CollectionAssert.AreEqual(_blob, block.DataBytes);
    }

    [TestMethod]
    public async Task Put_Informs_Bitswap()
    {
        var data = Guid.NewGuid().ToByteArray();
        var cid = new Cid { Hash = MultiHash.ComputeHash(data) };
        var wantTask = _ipfs.Bitswap.GetAsync(cid);

        var cid1 = await _ipfs.Block.PutAsync(data);
        Assert.AreEqual(cid, cid1);
        Assert.IsTrue(wantTask.IsCompleted);
        Assert.AreEqual(cid, wantTask.Result.Id);
        Assert.AreEqual(data.Length, wantTask.Result.Size);
        CollectionAssert.AreEqual(data, wantTask.Result.DataBytes);
    }

    [TestMethod]
    public async Task Put_Informs_Dht()
    {
        var data = Guid.NewGuid().ToByteArray();
        var ipfs = TestFixture.Ipfs;
        await ipfs.StartAsync();
        try
        {
            var self = await ipfs.LocalPeer;
            var cid = await ipfs.Block.PutAsync(data);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var peers = await ipfs.Dht.FindProvidersAsync(cid, 1, cancel: cts.Token);
            Assert.AreEqual(self, peers.First());
        }
        finally
        {
            await ipfs.StopAsync();
        }
    }
}