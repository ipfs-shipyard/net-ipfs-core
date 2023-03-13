using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk.PubSub;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.PeerTalk.Tests.PubSub;

[TestClass]
public class NotificationServiceTest
{
    private readonly Peer _self = new()
    {
        AgentVersion = "self",
        Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
        PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
    };

    private readonly Peer _other1 = new() { Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ" };
    private readonly Peer _other2 = new() { Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvUJ" };

    [TestMethod]
    public async Task MessageID_Increments()
    {
        var ns = new NotificationService { LocalPeer = _self };
        await ns.StartAsync();
        try
        {
            var a = ns.CreateMessage("topic", Array.Empty<byte>());
            var b = ns.CreateMessage("topic", Array.Empty<byte>());
            Assert.IsTrue(string.Compare(b.MessageId, a.MessageId, StringComparison.Ordinal) > 0);
        }
        finally
        {
            await ns.StopAsync();
        }
    }

    [TestMethod]
    public async Task Publish()
    {
        var ns = new NotificationService { LocalPeer = _self };
        await ns.StartAsync();
        try
        {
            await ns.PublishAsync("topic", "foo");
            await ns.PublishAsync("topic", new byte[] { 1, 2, 3 });
            await ns.PublishAsync("topic", new MemoryStream(new byte[] { 1, 2, 3 }));
            Assert.AreEqual(3ul, ns.MesssagesPublished);
            Assert.AreEqual(3ul, ns.MesssagesReceived);
        }
        finally
        {
            await ns.StopAsync();
        }
    }

    [TestMethod]
    public async Task Topics()
    {
        var ns = new NotificationService { LocalPeer = _self };
        await ns.StartAsync();
        try
        {
            var topicA = Guid.NewGuid().ToString();
            var topicB = Guid.NewGuid().ToString();
            var csA = new CancellationTokenSource();
            var csB = new CancellationTokenSource();

            await ns.SubscribeAsync(topicA, _ => { }, csA.Token);
            await ns.SubscribeAsync(topicA, _ => { }, csA.Token);
            await ns.SubscribeAsync(topicB, _ => { }, csB.Token);

            var topics = (await ns.SubscribedTopicsAsync()).ToArray();
            Assert.AreEqual(2, topics.Length);
            CollectionAssert.Contains(topics, topicA);
            CollectionAssert.Contains(topics, topicB);

            csA.Cancel();
            topics = (await ns.SubscribedTopicsAsync()).ToArray();
            Assert.AreEqual(1, topics.Length);
            CollectionAssert.Contains(topics, topicB);

            csB.Cancel();
            topics = (await ns.SubscribedTopicsAsync()).ToArray();
            Assert.AreEqual(0, topics.Length);
        }
        finally
        {
            await ns.StopAsync();
        }
    }

    [TestMethod]
    public async Task Subscribe()
    {
        var ns = new NotificationService { LocalPeer = _self };
        await ns.StartAsync();
        try
        {
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            var messageCount = 0;
            await ns.SubscribeAsync(topic, _ => { ++messageCount; }, cs.Token);
            await ns.SubscribeAsync(topic, _ => { ++messageCount; }, cs.Token);

            await ns.PublishAsync(topic, "", cs.Token);
            Assert.AreEqual(2, messageCount);
        }
        finally
        {
            await ns.StopAsync();
        }
    }

    [TestMethod]
    public async Task Subscribe_HandlerExceptionIsIgnored()
    {
        var ns = new NotificationService { LocalPeer = _self };
        await ns.StartAsync();
        try
        {
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            var messageCount = 0;
            await ns.SubscribeAsync(topic, _ => { ++messageCount; throw new(); }, cs.Token);

            await ns.PublishAsync(topic, "", cs.Token);
            Assert.AreEqual(1, messageCount);
        }
        finally
        {
            await ns.StopAsync();
        }
    }

    [TestMethod]
    public async Task DuplicateMessagesAreIgnored()
    {
        var ns = new NotificationService { LocalPeer = _self };
        ns.Routers.Add(new LoopbackRouter());
        await ns.StartAsync();
        try
        {
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            var messageCount = 0;
            await ns.SubscribeAsync(topic, _ => { ++messageCount; }, cs.Token);

            await ns.PublishAsync(topic, "", cs.Token);
            Assert.AreEqual(1, messageCount);
            Assert.AreEqual(2ul, ns.MesssagesReceived);
            Assert.AreEqual(1ul, ns.DuplicateMesssagesReceived);
        }
        finally
        {
            await ns.StopAsync();
        }
    }

    [TestMethod]
    public async Task SubscribedPeers_ForTopic()
    {
        var topic1 = Guid.NewGuid().ToString();
        var topic2 = Guid.NewGuid().ToString();
        var ns = new NotificationService { LocalPeer = _self };
        var router = new FloodRouter { Swarm = new() };
        router.RemoteTopics.AddInterest(topic1, _other1);
        router.RemoteTopics.AddInterest(topic2, _other2);
        ns.Routers.Add(router);
        await ns.StartAsync();
        try
        {
            var peers = (await ns.PeersAsync(topic1)).ToArray();
            Assert.AreEqual(1, peers.Length);
            Assert.AreEqual(_other1, peers[0]);

            peers = (await ns.PeersAsync(topic2)).ToArray();
            Assert.AreEqual(1, peers.Length);
            Assert.AreEqual(_other2, peers[0]);
        }
        finally
        {
            await ns.StopAsync();
        }
    }

    [TestMethod]
    public async Task SubscribedPeers_AllTopics()
    {
        var topic1 = Guid.NewGuid().ToString();
        var topic2 = Guid.NewGuid().ToString();
        var ns = new NotificationService { LocalPeer = _self };
        var router = new FloodRouter { Swarm = new() };
        router.RemoteTopics.AddInterest(topic1, _other1);
        router.RemoteTopics.AddInterest(topic2, _other2);
        ns.Routers.Add(router);
        await ns.StartAsync();
        try
        {
            var peers = (await ns.PeersAsync()).ToArray();
            Assert.AreEqual(2, peers.Length);
        }
        finally
        {
            await ns.StopAsync();
        }
    }
}