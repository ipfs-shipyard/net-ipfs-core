using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Http.Tests.CoreApi;

[TestClass]
public class PubSubApiTest
{
    [TestMethod]
    public void Api_Exists()
    {
        var ipfs = TestFixture.Ipfs;
        Assert.IsNotNull(ipfs.PubSub);
    }

    [TestMethod]
    public async Task Peers()
    {
        var ipfs = TestFixture.Ipfs;
        var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
        var cs = new CancellationTokenSource();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, _ => { }, cs.Token);
            var peers = ipfs.PubSub.PeersAsync(cancel: cs.Token).Result.ToArray();
            Assert.IsTrue(peers.Length > 0);
        }
        finally
        {
            cs.Cancel();
        }
    }

    [TestMethod]
    public void Peers_Unknown_Topic()
    {
        var ipfs = TestFixture.Ipfs;
        var topic = "net-ipfs-http-client-test-unknown" + Guid.NewGuid();
        var peers = ipfs.PubSub.PeersAsync(topic).Result.ToArray();
        Assert.AreEqual(0, peers.Length);
    }

    [TestMethod]
    public async Task Subscribed_Topics()
    {
        var ipfs = TestFixture.Ipfs;
        var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
        var cs = new CancellationTokenSource();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, _ => { }, cs.Token);
            var topics = ipfs.PubSub.SubscribedTopicsAsync(cs.Token).Result.ToArray();
            Assert.IsTrue(topics.Length > 0);
            CollectionAssert.Contains(topics, topic);
        }
        finally
        {
            cs.Cancel();
        }
    }

    private volatile int _messageCount;

    [TestMethod]
    public async Task Subscribe()
    {
        _messageCount = 0;
        var ipfs = TestFixture.Ipfs;
        var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
        var cs = new CancellationTokenSource();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, _ =>
            {
                Interlocked.Increment(ref _messageCount);
            }, cs.Token);
            await ipfs.PubSub.PublishAsync(topic, "hello world!", cs.Token);

            await Task.Delay(1000, cs.Token);
            Assert.AreEqual(1, _messageCount);
        }
        finally
        {
            cs.Cancel();
        }
    }

    [TestMethod]
    public async Task Subscribe_Mutiple_Messages()
    {
        _messageCount = 0;
        var messages = "hello world this is pubsub".Split();
        var ipfs = TestFixture.Ipfs;
        var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
        var cs = new CancellationTokenSource();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, _ =>
            {
                Interlocked.Increment(ref _messageCount);
            }, cs.Token);
            foreach (var msg in messages)
            {
                await ipfs.PubSub.PublishAsync(topic, msg, cs.Token);
            }

            await Task.Delay(1000, cs.Token);
            Assert.AreEqual(messages.Length, _messageCount);
        }
        finally
        {
            cs.Cancel();
        }
    }

    [TestMethod]
    public async Task Multiple_Subscribe_Multiple_Messages()
    {
        _messageCount = 0;
        var messages = "hello world this is pubsub".Split();
        var ipfs = TestFixture.Ipfs;
        var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
        var cs = new CancellationTokenSource();
        Action<IPublishedMessage> processMessage = _ =>
        {
            Interlocked.Increment(ref _messageCount);
        };
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
            await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
            foreach (var msg in messages)
            {
                await ipfs.PubSub.PublishAsync(topic, msg, cs.Token);
            }

            await Task.Delay(1000, cs.Token);
            Assert.AreEqual(messages.Length * 2, _messageCount);
        }
        finally
        {
            cs.Cancel();
        }
    }

    private volatile int _messageCount1;

    [TestMethod]
    public async Task Unsubscribe()
    {
        _messageCount1 = 0;
        var ipfs = TestFixture.Ipfs;
        var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
        var cs = new CancellationTokenSource();
        await ipfs.PubSub.SubscribeAsync(topic, _ =>
        {
            Interlocked.Increment(ref _messageCount1);
        }, cs.Token);
        await ipfs.PubSub.PublishAsync(topic, "hello world!", cs.Token);
        await Task.Delay(1000, cs.Token);
        Assert.AreEqual(1, _messageCount1);

        cs.Cancel();
        await ipfs.PubSub.PublishAsync(topic, "hello world!!!", cs.Token);
        await Task.Delay(1000, cs.Token);
        Assert.AreEqual(1, _messageCount1);
    }

    [TestMethod]
    public async Task Subscribe_BinaryMessage()
    {
        var messages = new List<IPublishedMessage>();
        var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
        var ipfs = TestFixture.Ipfs;
        var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
        var cs = new CancellationTokenSource();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, msg =>
            {
                messages.Add(msg);
            }, cs.Token);
            await ipfs.PubSub.PublishAsync(topic, expected, cs.Token);

            await Task.Delay(1000, cs.Token);
            Assert.AreEqual(1, messages.Count);
            CollectionAssert.AreEqual(expected, messages[0].DataBytes);
        }
        finally
        {
            cs.Cancel();
        }
    }

    [TestMethod]
    public async Task Subscribe_StreamMessage()
    {
        var messages = new List<IPublishedMessage>();
        var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
        var ipfs = TestFixture.Ipfs;
        var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
        var cs = new CancellationTokenSource();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, msg =>
            {
                messages.Add(msg);
            }, cs.Token);
            var ms = new MemoryStream(expected, false);
            await ipfs.PubSub.PublishAsync(topic, ms, cs.Token);

            await Task.Delay(1000, cs.Token);
            Assert.AreEqual(1, messages.Count);
            CollectionAssert.AreEqual(expected, messages[0].DataBytes);
        }
        finally
        {
            cs.Cancel();
        }
    }
}