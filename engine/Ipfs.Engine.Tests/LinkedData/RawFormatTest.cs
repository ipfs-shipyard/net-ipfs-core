using System;
using System.Text;
using Ipfs.Engine.LinkedData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests.LinkedData;

[TestClass]
public class RawFormatTest
{
    private readonly ILinkedDataFormat _formatter = new RawFormat();

    [TestMethod]
    public void Empty()
    {
        var data = Array.Empty<byte>();

        var cbor = _formatter.Deserialise(data);
        CollectionAssert.AreEqual(data, cbor["data"].GetByteString());

        var data1 = _formatter.Serialize(cbor);
        CollectionAssert.AreEqual(data, data1);
    }

    [TestMethod]
    public void Data()
    {
        var data = "abc"u8.ToArray();

        var cbor = _formatter.Deserialise(data);
        CollectionAssert.AreEqual(data, cbor["data"].GetByteString());

        var data1 = _formatter.Serialize(cbor);
        CollectionAssert.AreEqual(data, data1);
    }
}