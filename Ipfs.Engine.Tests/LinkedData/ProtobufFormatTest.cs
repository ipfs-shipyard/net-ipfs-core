using System;
using Ipfs.Engine.LinkedData;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests.LinkedData;

[TestClass]
public class ProtobufFormatTest
{
    private readonly ILinkedDataFormat _formatter = new ProtobufFormat();

    [TestMethod]
    public void Empty()
    {
        var data = Array.Empty<byte>();
        var node = new DagNode(data);

        var cbor = _formatter.Deserialise(node.ToArray());
        CollectionAssert.AreEqual(data, cbor["data"].GetByteString());
        Assert.AreEqual(0, cbor["links"].Values.Count);

        var node1 = _formatter.Serialize(cbor);
        CollectionAssert.AreEqual(node.ToArray(), node1);
    }

    [TestMethod]
    public void DataOnly()
    {
        var data = "abc"u8.ToArray();
        var node = new DagNode(data);

        var cbor = _formatter.Deserialise(node.ToArray());
        CollectionAssert.AreEqual(data, cbor["data"].GetByteString());
        Assert.AreEqual(0, cbor["links"].Values.Count);

        var node1 = _formatter.Serialize(cbor);
        CollectionAssert.AreEqual(node.ToArray(), node1);
    }

    [TestMethod]
    public void LinksOnly()
    {
        var a = "a"u8.ToArray();
        var anode = new DagNode(a);
        var alink = anode.ToLink("a");

        var b = "b"u8.ToArray();
        var bnode = new DagNode(b);
        var blink = bnode.ToLink();

        var node = new DagNode(null, new[] { alink, blink });
        var cbor = _formatter.Deserialise(node.ToArray());

        Assert.AreEqual(2, cbor["links"].Values.Count);

        var link = cbor["links"][0];
        Assert.AreEqual("QmYpoNmG5SWACYfXsDztDNHs29WiJdmP7yfcMd7oVa75Qv", link["Cid"]["/"].AsString());
        Assert.AreEqual("", link["Name"].AsString());
        Assert.AreEqual(3, link["Size"].AsInt32());

        link = cbor["links"][1];
        Assert.AreEqual("QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", link["Cid"]["/"].AsString());
        Assert.AreEqual("a", link["Name"].AsString());
        Assert.AreEqual(3, link["Size"].AsInt32());

        var node1 = _formatter.Serialize(cbor);
        CollectionAssert.AreEqual(node.ToArray(), node1);
    }
}