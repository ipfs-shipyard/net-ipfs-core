using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.PubSub
{
    
    [TestClass]
    public class PublishedMessageTest
    {
        Peer self = new Peer { Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH" };
        Peer other = new Peer { Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ" };

        [TestMethod]
        public void RoundTrip()
        {
            var a = new PublishedMessage
            {
                Topics = new string[] { "topic" },
                Sender = self,
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
        }

        [TestMethod]
        public void MessageID_Is_Unique()
        {
            var a = new PublishedMessage
            {
                Topics = new string[] { "topic" },
                Sender = self,
                SequenceNumber = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                DataBytes = new byte[] { 0, 1, 0xfe, 0xff }
            };
            var b = new PublishedMessage
            {
                Topics = new string[] { "topic" },
                Sender = other,
                SequenceNumber = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                DataBytes = new byte[] { 0, 1, 0xfe, 0xff }
            };

            Assert.AreNotEqual(a.MessageId, b.MessageId);
        }
    }
}
