using System;
using System.IO;
using System.Linq;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk.PubSub;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;

namespace IpfsShipyard.PeerTalk.Tests.PubSub;

[TestClass]
public class PublishedMessageTest
{
    private readonly Peer _self = new Peer { Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH" };
    private readonly Peer _other = new Peer { Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ" };

    [TestMethod]
    public void RoundTrip()
    {
        var a = new PublishedMessage
        {
            Topics = new string[] { "topic" },
            Sender = _self,
            SequenceNumber = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8},
            DataBytes = new byte[] { 0, 1, 0xfe, 0xff }
        };
        var ms = new MemoryStream();
        Serializer.Serialize(ms, a);
        ms.Position = 0;
        var b = Serializer.Deserialize<PublishedMessage>(ms); ;

        CollectionAssert.AreEqual(a.Topics.ToArray(), b.Topics.ToArray());
        Assert.AreEqual(a.Sender, b.Sender);
        CollectionAssert.AreEqual(a.SequenceNumber, b.SequenceNumber);
        CollectionAssert.AreEqual(a.DataBytes, b.DataBytes);
        Assert.AreEqual(a.DataBytes.Length, a.Size);
        Assert.AreEqual(b.DataBytes.Length, b.Size);
    }

    [TestMethod]
    public void MessageID_Is_Unique()
    {
        var a = new PublishedMessage
        {
            Topics = new string[] { "topic" },
            Sender = _self,
            SequenceNumber = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            DataBytes = new byte[] { 0, 1, 0xfe, 0xff }
        };
        var b = new PublishedMessage
        {
            Topics = new string[] { "topic" },
            Sender = _other,
            SequenceNumber = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            DataBytes = new byte[] { 0, 1, 0xfe, 0xff }
        };

        Assert.AreNotEqual(a.MessageId, b.MessageId);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void CidNotSupported()
    {
        var _ = new PublishedMessage().Id;
    }

    [TestMethod]
    public void DataStream()
    {
        var msg = new PublishedMessage { DataBytes = new byte[] { 1 } };
        Assert.AreEqual(1, msg.DataStream.ReadByte());
    }
}