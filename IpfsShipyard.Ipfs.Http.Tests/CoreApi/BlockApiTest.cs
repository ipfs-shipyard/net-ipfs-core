using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Http.Tests.CoreApi;

[TestClass]
public class BlockApiTest
{
    private readonly IpfsClient _ipfs = TestFixture.Ipfs;
    private readonly string _id = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";
    private readonly byte[] _blob = "blorb"u8.ToArray();

    [TestMethod]
    public void Put_Bytes()
    {
        var cid = _ipfs.Block.PutAsync(_blob).Result;
        Assert.AreEqual(_id, (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual<long>(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Bytes_ContentType()
    {
        var cid = _ipfs.Block.PutAsync(_blob, contentType: "raw").Result;
        Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual<long>(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Bytes_Hash()
    {
        var cid = _ipfs.Block.PutAsync(_blob, "raw", "sha2-512").Result;
        Assert.AreEqual("bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e", (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual<long>(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Bytes_Pinned()
    {
        var data1 = new byte[] { 23, 24, 127 };
        var cid1 = _ipfs.Block.PutAsync(data1, contentType: "raw", pin: true).Result;
        var pins = _ipfs.Pin.ListAsync().Result;
        Assert.IsTrue(Enumerable.Any<Cid>(pins, pin => pin == cid1));

        var data2 = new byte[] { 123, 124, 27 };
        var cid2 = _ipfs.Block.PutAsync(data2, contentType: "raw", pin: false).Result;
        pins = _ipfs.Pin.ListAsync().Result;
        Assert.IsFalse(Enumerable.Any<Cid>(pins, pin => pin == cid2));
    }

    [TestMethod]
    public void Put_Stream()
    {
        var cid = _ipfs.Block.PutAsync(new MemoryStream(_blob)).Result;
        Assert.AreEqual(_id, (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual<long>(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Stream_ContentType()
    {
        var cid = _ipfs.Block.PutAsync(new MemoryStream(_blob), contentType: "raw").Result;
        Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual<long>(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Stream_Hash()
    {
        var cid = _ipfs.Block.PutAsync(new MemoryStream(_blob), "raw", "sha2-512").Result;
        Assert.AreEqual("bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e", (string)cid);

        var data = _ipfs.Block.GetAsync(cid).Result;
        Assert.AreEqual<long>(_blob.Length, data.Size);
        CollectionAssert.AreEqual(_blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Stream_Pinned()
    {
        var data1 = new MemoryStream(new byte[] { 23, 24, 127 });
        var cid1 = _ipfs.Block.PutAsync(data1, contentType: "raw", pin: true).Result;
        var pins = _ipfs.Pin.ListAsync().Result;
        Assert.IsTrue(Enumerable.Any<Cid>(pins, pin => pin == cid1));

        var data2 = new MemoryStream(new byte[] { 123, 124, 27 });
        var cid2 = _ipfs.Block.PutAsync(data2, contentType: "raw", pin: false).Result;
        pins = _ipfs.Pin.ListAsync().Result;
        Assert.IsFalse(Enumerable.Any<Cid>(pins, pin => pin == cid2));
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
        Assert.AreEqual<long>(5, info.Size);
    }

    [TestMethod]
    public async Task Remove()
    {
        var _ = _ipfs.Block.PutAsync(_blob).Result;
        var cid = await _ipfs.Block.RemoveAsync(_id);
        Assert.AreEqual(_id, (string)cid);
    }

    [TestMethod]
    public void Remove_Unknown()
    {
        ExceptionAssert.Throws<Exception>(() => { var _ = _ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF").Result; });
    }

    [TestMethod]
    public async Task Remove_Unknown_OK()
    {
        var cid = await _ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF", true);
    }

}