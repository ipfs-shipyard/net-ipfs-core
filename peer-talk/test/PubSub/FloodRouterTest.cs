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
    public class FloodRouterTest
    {
        Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };
        Peer other = new Peer
        {
            AgentVersion = "other",
            Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
            PublicKey = "CAASpgIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCfBYU9c0n28u02N/XCJY8yIsRqRVO5Zw+6kDHCremt2flHT4AaWnwGLAG9YyQJbRTvWN9nW2LK7Pv3uoIlvUSTnZEP0SXB5oZeqtxUdi6tuvcyqTIfsUSanLQucYITq8Qw3IMBzk+KpWNm98g9A/Xy30MkUS8mrBIO9pHmIZa55fvclDkTvLxjnGWA2avaBfJvHgMSTu0D2CQcmJrvwyKMhLCSIbQewZd2V7vc6gtxbRovKlrIwDTmDBXbfjbLljOuzg2yBLyYxXlozO9blpttbnOpU4kTspUVJXglmjsv7YSIJS3UKt3544l/srHbqlwC5CgOgjlwNfYPadO8kmBfAgMBAAE="
        };

        [TestMethod]
        public void Defaults()
        {
            var router = new FloodRouter();
            Assert.AreEqual("/floodsub/1.0.0", router.ToString());
        }

        [TestMethod]
        public void RemoteSubscriptions()
        {
            var router = new FloodRouter();

            var sub = new Subscription { Topic = "topic", Subscribe = true };
            router.ProcessSubscription(sub, other);
            Assert.AreEqual(1, router.RemoteTopics.GetPeers("topic").Count());

            var can = new Subscription { Topic = "topic", Subscribe = false };
            router.ProcessSubscription(can, other);
            Assert.AreEqual(0, router.RemoteTopics.GetPeers("topic").Count());
        }

        [TestMethod]
        public async Task Sends_Hello_OnConnect()
        {
            var topic = Guid.NewGuid().ToString();

            var swarm1 = new Swarm { LocalPeer = self };
            var router1 = new FloodRouter { Swarm = swarm1 };
            var ns1 = new NotificationService { LocalPeer = self };
            ns1.Routers.Add(router1);
            await swarm1.StartAsync();
            await ns1.StartAsync();

            var swarm2 = new Swarm { LocalPeer = other };
            var router2 = new FloodRouter { Swarm = swarm2 };
            var ns2 = new NotificationService { LocalPeer = other };
            ns2.Routers.Add(router2);
            await swarm2.StartAsync();
            await ns2.StartAsync();

            try
            {
                await swarm1.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                await swarm2.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

                var cs = new CancellationTokenSource();
                await ns1.SubscribeAsync(topic, msg => { }, cs.Token);
                await swarm1.ConnectAsync(other);

                Peer[] peers = new Peer[0];
                var endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length == 0)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("timeout");
                    await Task.Delay(100);
                    peers = (await ns2.PeersAsync(topic)).ToArray();
                }
                CollectionAssert.Contains(peers, self);
            }
            finally
            {
                await swarm1.StopAsync();
                await ns1.StopAsync();

                await swarm2.StopAsync();
                await ns2.StopAsync();
            }
        }

        [TestMethod]
        public async Task Sends_NewSubscription()
        {
            var topic = Guid.NewGuid().ToString();

            var swarm1 = new Swarm { LocalPeer = self };
            var router1 = new FloodRouter { Swarm = swarm1 };
            var ns1 = new NotificationService { LocalPeer = self };
            ns1.Routers.Add(router1);
            await swarm1.StartAsync();
            await ns1.StartAsync();

            var swarm2 = new Swarm { LocalPeer = other };
            var router2 = new FloodRouter { Swarm = swarm2 };
            var ns2 = new NotificationService { LocalPeer = other };
            ns2.Routers.Add(router2);
            await swarm2.StartAsync();
            await ns2.StartAsync();

            try
            {
                await swarm1.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                await swarm2.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

                var cs = new CancellationTokenSource();
                await swarm1.ConnectAsync(other);
                await ns1.SubscribeAsync(topic, msg => { }, cs.Token);

                Peer[] peers = new Peer[0];
                var endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length == 0)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("timeout");
                    await Task.Delay(100);
                    peers = (await ns2.PeersAsync(topic)).ToArray();
                }
                CollectionAssert.Contains(peers, self);
            }
            finally
            {
                await swarm1.StopAsync();
                await ns1.StopAsync();

                await swarm2.StopAsync();
                await ns2.StopAsync();
            }
        }

        [TestMethod]
        public async Task Sends_CancelledSubscription()
        {
            var topic = Guid.NewGuid().ToString();

            var swarm1 = new Swarm { LocalPeer = self };
            var router1 = new FloodRouter { Swarm = swarm1 };
            var ns1 = new NotificationService { LocalPeer = self };
            ns1.Routers.Add(router1);
            await swarm1.StartAsync();
            await ns1.StartAsync();

            var swarm2 = new Swarm { LocalPeer = other };
            var router2 = new FloodRouter { Swarm = swarm2 };
            var ns2 = new NotificationService { LocalPeer = other };
            ns2.Routers.Add(router2);
            await swarm2.StartAsync();
            await ns2.StartAsync();

            try
            {
                await swarm1.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                await swarm2.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

                var cs = new CancellationTokenSource();
                await swarm1.ConnectAsync(other);
                await ns1.SubscribeAsync(topic, msg => { }, cs.Token);

                Peer[] peers = new Peer[0];
                var endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length == 0)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("timeout");
                    await Task.Delay(100);
                    peers = (await ns2.PeersAsync(topic)).ToArray();
                }
                CollectionAssert.Contains(peers, self);

                cs.Cancel();
                peers = new Peer[0];
                endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length != 0)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("timeout");
                    await Task.Delay(100);
                    peers = (await ns2.PeersAsync(topic)).ToArray();
                }
            }
            finally
            {
                await swarm1.StopAsync();
                await ns1.StopAsync();

                await swarm2.StopAsync();
                await ns2.StopAsync();
            }
        }

        [TestMethod]
        public async Task Gets_PublishedMessage()
        {
            var topic = Guid.NewGuid().ToString();

            var swarm1 = new Swarm { LocalPeer = self };
            var router1 = new FloodRouter { Swarm = swarm1 };
            var ns1 = new NotificationService { LocalPeer = self };
            ns1.Routers.Add(router1);
            await swarm1.StartAsync();
            await ns1.StartAsync();

            var swarm2 = new Swarm { LocalPeer = other };
            var router2 = new FloodRouter { Swarm = swarm2 };
            var ns2 = new NotificationService { LocalPeer = other };
            ns2.Routers.Add(router2);
            await swarm2.StartAsync();
            await ns2.StartAsync();

            try
            {
                IPublishedMessage lastMessage = null;
                await swarm1.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                await swarm2.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

                var cs = new CancellationTokenSource();
                await ns1.SubscribeAsync(topic, msg => lastMessage = msg, cs.Token);
                await swarm1.ConnectAsync(other);

                Peer[] peers = new Peer[0];
                var endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length == 0)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("timeout");
                    await Task.Delay(100);
                    peers = (await ns2.PeersAsync(topic)).ToArray();
                }
                CollectionAssert.Contains(peers, self);

                await ns2.PublishAsync(topic, new byte[] { 1 });
                endTime = DateTime.Now.AddSeconds(3);
                while (lastMessage == null)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("timeout");
                    await Task.Delay(100);
                }

                Assert.IsNotNull(lastMessage);
                Assert.AreEqual(other, lastMessage.Sender);
                CollectionAssert.AreEqual(new byte[] { 1 }, lastMessage.DataBytes);
                CollectionAssert.Contains(lastMessage.Topics.ToArray(), topic);
            }
            finally
            {
                await swarm1.StopAsync();
                await ns1.StopAsync();

                await swarm2.StopAsync();
                await ns2.StopAsync();
            }
        }
    }
}
