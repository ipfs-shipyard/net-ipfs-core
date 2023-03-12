using System;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class DagApiTest
{
    private readonly byte[] _blob = "blorb"u8.ToArray();
    private readonly string _blob64 = "YmxvcmI"; // base 64 encoded with no padding
    private readonly IpfsEngine _ipfs = TestFixture.Ipfs;

    [TestMethod]
    public async Task Get_Raw()
    {
        var cid = await _ipfs.Block.PutAsync(_blob, "raw");
        Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string)cid);

        var dag = await _ipfs.Dag.GetAsync(cid);
        Assert.AreEqual(_blob64, (string)dag["data"]);
    }

    [TestMethod]
    public async Task PutAndGet_JSON()
    {
        var expected = new JObject
        {
            ["a"] = "alpha"
        };
        var expectedId = "bafyreigdhej736dobd6z3jt2vxsxvbwrwgyts7e7wms6yrr46rp72uh5bu";
        var id = await _ipfs.Dag.PutAsync(expected);
        Assert.IsNotNull(id);
        Assert.AreEqual(expectedId, (string)id);

        var actual = await _ipfs.Dag.GetAsync(id);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected["a"], actual["a"]);

        var value = (string)await _ipfs.Dag.GetAsync(expectedId + "/a");
        Assert.AreEqual(expected["a"], value);
    }

    [TestMethod]
    public async Task PutAndGet_poco()
    {
        var expected = new name { first = "John", last = "Smith" };
        var id = await _ipfs.Dag.PutAsync(expected);
        Assert.IsNotNull(id);

        var actual = await _ipfs.Dag.GetAsync<name>(id);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.first, actual.first);
        Assert.AreEqual(expected.last, actual.last);

        var value = (string)await _ipfs.Dag.GetAsync(id.Encode() + "/last");
        Assert.AreEqual(expected.last, value);
    }

    [TestMethod]
    public async Task PutAndGet_poco_CidEncoding()
    {
        var expected = new name { first = "John", last = "Smith" };
        var id = await _ipfs.Dag.PutAsync(expected, encoding: "base32");
        Assert.IsNotNull(id);
        Assert.AreEqual("base32", id.Encoding);
        Assert.AreEqual(1, id.Version);

        var actual = await _ipfs.Dag.GetAsync<name>(id);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.first, actual.first);
        Assert.AreEqual(expected.last, actual.last);

        var value = (string)await _ipfs.Dag.GetAsync(id.Encode() + "/last");
        Assert.AreEqual(expected.last, value);
    }

    [TestMethod]
    public async Task PutAndGet_POCO()
    {
        var expected = new Name { First = "John", Last = "Smith" };
        var id = await _ipfs.Dag.PutAsync(expected);
        Assert.IsNotNull(id);

        var actual = await _ipfs.Dag.GetAsync<Name>(id);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.First, actual.First);
        Assert.AreEqual(expected.Last, actual.Last);

        var value = (string)await _ipfs.Dag.GetAsync(id.Encode() + "/Last");
        Assert.AreEqual(expected.Last, value);
    }

    [TestMethod]
    public async Task Get_Raw2()
    {
        var data = "abc"u8.ToArray();
        var id = await _ipfs.Block.PutAsync(data, "raw");
        Assert.AreEqual("bafkreif2pall7dybz7vecqka3zo24irdwabwdi4wc55jznaq75q7eaavvu", id.Encode());

        var actual = await _ipfs.Dag.GetAsync(id);
        Assert.AreEqual(Convert.ToBase64String(data), (string)actual["data"]);
    }

    // https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/DAG.md
    [TestMethod]
    [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/30")]
    public async Task Example1()
    {
        Cid expected = "zBwWX9ecx5F4X54WAjmFLErnBT6ByfNxStr5ovowTL7AhaUR98RWvXPS1V3HqV1qs3r5Ec5ocv7eCdbqYQREXNUfYNuKG";
        var obj = new { simple = "object" };
        var cid = await _ipfs.Dag.PutAsync(obj, multiHash: "sha3-512");
        Assert.AreEqual((string)expected, (string)cid);
    }

    private record Name
    {
        public string First { get; init; }
        public string Last { get; init; }
    }

    private record name
    {
        public string first { get; init; }
        public string last { get; init; }
    }
}