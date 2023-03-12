using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class PubSubApiTest
{
    private volatile int _messageCount;

    private volatile int _messageCount1;

    [TestMethod]
    public void Api_Exists()
    {
        var ipfs = TestFixture.Ipfs;
        Assert.IsNotNull(ipfs.PubSub);
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
        var topic = Guid.NewGuid().ToString();
        var cs = new CancellationTokenSource();
        await ipfs.StartAsync();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, msg => { }, cs.Token);
            var topics = ipfs.PubSub.SubscribedTopicsAsync(cs.Token).Result.ToArray();
            Assert.IsTrue(topics.Length > 0);
            CollectionAssert.Contains(topics, topic);
        }
        finally
        {
            await ipfs.StopAsync();
            cs.Cancel();
        }
    }

    [TestMethod]
    public async Task Subscribe()
    {
        _messageCount = 0;
        var ipfs = TestFixture.Ipfs;
        var topic = Guid.NewGuid().ToString();
        var cs = new CancellationTokenSource();
        await ipfs.StartAsync();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, msg => { Interlocked.Increment(ref _messageCount); }, cs.Token);
            await ipfs.PubSub.PublishAsync(topic, "hello world!", cs.Token);

            await Task.Delay(100, cs.Token);
            Assert.AreEqual(1, _messageCount);
        }
        finally
        {
            await ipfs.StopAsync();
            cs.Cancel();
        }
    }

    [TestMethod]
    public async Task Subscribe_Mutiple_Messages()
    {
        _messageCount = 0;
        var messages = "hello world this is pubsub".Split();
        var ipfs = TestFixture.Ipfs;
        var topic = Guid.NewGuid().ToString();
        var cs = new CancellationTokenSource();
        await ipfs.StartAsync();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, msg => { Interlocked.Increment(ref _messageCount); }, cs.Token);
            foreach (var msg in messages)
            {
                await ipfs.PubSub.PublishAsync(topic, msg, cs.Token);
            }

            await Task.Delay(100, cs.Token);
            Assert.AreEqual(messages.Length, _messageCount);
        }
        finally
        {
            await ipfs.StopAsync();
            cs.Cancel();
        }
    }

    [TestMethod]
    public async Task Multiple_Subscribe_Mutiple_Messages()
    {
        _messageCount = 0;
        var messages = "hello world this is pubsub".Split();
        var ipfs = TestFixture.Ipfs;
        var topic = Guid.NewGuid().ToString();
        var cs = new CancellationTokenSource();

        void ProcessMessage(IPublishedMessage msg)
        {
            Interlocked.Increment(ref _messageCount);
        }

        await ipfs.StartAsync();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, ProcessMessage, cs.Token);
            await ipfs.PubSub.SubscribeAsync(topic, ProcessMessage, cs.Token);
            foreach (var msg in messages)
            {
                await ipfs.PubSub.PublishAsync(topic, msg, cs.Token);
            }

            await Task.Delay(100, cs.Token);
            Assert.AreEqual(messages.Length * 2, _messageCount);
        }
        finally
        {
            await ipfs.StopAsync();
            cs.Cancel();
        }
    }

    [TestMethod]
    public async Task Unsubscribe()
    {
        _messageCount1 = 0;
        var ipfs = TestFixture.Ipfs;
        var topic = Guid.NewGuid().ToString();
        var cs = new CancellationTokenSource();
        await ipfs.StartAsync();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, msg => { Interlocked.Increment(ref _messageCount1); }, cs.Token);
            await ipfs.PubSub.PublishAsync(topic, "hello world!", cs.Token);
            await Task.Delay(100, cs.Token);
            Assert.AreEqual(1, _messageCount1);

            cs.Cancel();
            await ipfs.PubSub.PublishAsync(topic, "hello world!!!", cs.Token);
            await Task.Delay(100, cs.Token);
            Assert.AreEqual(1, _messageCount1);
        }
        finally
        {
            await ipfs.StopAsync();
        }
    }

    [TestMethod]
    public async Task Subscribe_BinaryMessage()
    {
        var messages = new List<IPublishedMessage>();
        var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
        var ipfs = TestFixture.Ipfs;
        var topic = Guid.NewGuid().ToString();
        var cs = new CancellationTokenSource();
        await ipfs.StartAsync();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, msg => { messages.Add(msg); }, cs.Token);
            await ipfs.PubSub.PublishAsync(topic, expected, cs.Token);

            await Task.Delay(100, cs.Token);
            Assert.AreEqual(1, messages.Count);
            CollectionAssert.AreEqual(expected, messages[0].DataBytes);
        }
        finally
        {
            await ipfs.StopAsync();
            cs.Cancel();
        }
    }

    [TestMethod]
    public async Task Subscribe_StreamMessage()
    {
        var messages = new List<IPublishedMessage>();
        var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
        var ipfs = TestFixture.Ipfs;
        var topic = Guid.NewGuid().ToString();
        var cs = new CancellationTokenSource();
        await ipfs.StartAsync();
        try
        {
            await ipfs.PubSub.SubscribeAsync(topic, msg => { messages.Add(msg); }, cs.Token);
            var ms = new MemoryStream(expected, false);
            await ipfs.PubSub.PublishAsync(topic, ms, cs.Token);

            await Task.Delay(100, cs.Token);
            Assert.AreEqual(1, messages.Count);
            CollectionAssert.AreEqual(expected, messages[0].DataBytes);
        }
        finally
        {
            cs.Cancel();
            await ipfs.StopAsync();
        }
    }
}