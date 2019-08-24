﻿using Common.Logging;
using Ipfs;
using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.PubSub
{
    /// <summary>
    ///   A simple pub/sub messaging service that supports
    ///   multiple message routers.
    /// </summary>
    /// <remarks>
    ///   Relies upon the router(s) to deliver and receive messages from other peers.
    /// </remarks>
    public class NotificationService : IService, IPubSubApi
    {
        static ILog log = LogManager.GetLogger(typeof(NotificationService));

        class TopicHandler
        {
            public string Topic;
            public Action<IPublishedMessage> Handler;
        }
        
        long nextSequenceNumber;
        ConcurrentDictionary<TopicHandler, TopicHandler> topicHandlers;
        MessageTracker tracker = new MessageTracker();
        
        // TODO: A general purpose CancellationTokenSource that stops publishing of
        // messages when this service is stopped.

        /// <summary>
        ///   The local peer.
        /// </summary>
        public Peer LocalPeer { get; set; }

        /// <summary>
        ///   Sends and receives messages to other peers.
        /// </summary>
        public List<IMessageRouter> Routers { get; set; } = new List<IMessageRouter>
        {
            new LoopbackRouter()
        };

        /// <summary>
        ///   The number of messages that have published.
        /// </summary>
        public ulong MesssagesPublished;

        /// <summary>
        ///   The number of messages that have been received.
        /// </summary>
        public ulong MesssagesReceived;

        /// <summary>
        ///   The number of duplicate messages that have been received.
        /// </summary>
        public ulong DuplicateMesssagesReceived;

        /// <inheritdoc />
        public async Task StartAsync()
        {
            topicHandlers = new ConcurrentDictionary<TopicHandler, TopicHandler>();

            // Resolution of 100 nanoseconds.
            nextSequenceNumber = DateTime.UtcNow.Ticks;

            // Init the stats.
            MesssagesPublished = 0;
            MesssagesReceived = 0;
            DuplicateMesssagesReceived = 0;

            // Listen to the routers.
            foreach (var router in Routers)
            {
                router.MessageReceived += Router_MessageReceived;
                await router.StartAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task StopAsync()
        {
            topicHandlers.Clear();

            foreach (var router in Routers)
            {
                router.MessageReceived -= Router_MessageReceived;
                await router.StopAsync();
            }
        }

        /// <summary>
        ///   Creates a message for the topic and data.
        /// </summary>
        /// <param name="topic">
        ///   The topic name/id.
        /// </param>
        /// <param name="data">
        ///   The payload of message.
        /// </param>
        /// <returns>
        ///   A unique published message.
        /// </returns>
        /// <remarks>
        ///   The <see cref="PublishedMessage.SequenceNumber"/> is a monitonically 
        ///   increasing unsigned long.
        /// </remarks>
        public PublishedMessage CreateMessage(string topic, byte[] data)
        {
            var next = Interlocked.Increment(ref nextSequenceNumber);
            var seqno = BitConverter.GetBytes(next);
            if (BitConverter.IsLittleEndian)
            {
                seqno = seqno.Reverse().ToArray();
            }
            return new PublishedMessage
            {
                Topics = new string[] { topic },
                Sender = LocalPeer,
                SequenceNumber = seqno,
                DataBytes = data
            };
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel = default(CancellationToken))
        {
            var topics = topicHandlers.Values
                .Select(t => t.Topic)
                .Distinct();
            return Task.FromResult(topics);
        }

        /// <inheritdoc />
        public Task<IEnumerable<Peer>> PeersAsync(string topic = null, CancellationToken cancel = default(CancellationToken))
        {
            var peers = Routers
                .SelectMany(r => r.InterestedPeers(topic))
                .Distinct();
            return Task.FromResult(peers);
        }

        /// <inheritdoc />
        public Task PublishAsync(string topic, string message, CancellationToken cancel = default(CancellationToken))
        {
            return PublishAsync(topic, Encoding.UTF8.GetBytes(message), cancel);
        }

        /// <inheritdoc />
        public Task PublishAsync(string topic, Stream message, CancellationToken cancel = default(CancellationToken))
        {
            using (var ms = new MemoryStream())
            {
#pragma warning disable VSTHRD103
                message.CopyTo(ms);
#pragma warning disable VSTHRD103 
                return PublishAsync(topic, ms.ToArray(), cancel);
            }
        }

        /// <inheritdoc />
        public Task PublishAsync(string topic, byte[] message, CancellationToken cancel = default(CancellationToken))
        {
            var msg = CreateMessage(topic, message);
            ++MesssagesPublished;
            return Task
                .WhenAll(Routers.Select(r => r.PublishAsync(msg, cancel)));
        }

        /// <inheritdoc />
        public async Task SubscribeAsync(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken)
        {
            var topicHandler = new TopicHandler { Topic = topic, Handler = handler };
            topicHandlers.TryAdd(topicHandler, topicHandler);

            // TODO: need a better way.
#pragma warning disable VSTHRD101 
            cancellationToken.Register(async () =>
            {
                topicHandlers.TryRemove(topicHandler, out _);
                if (topicHandlers.Values.Count(t => t.Topic == topic) == 0)
                {
                    await Task.WhenAll(Routers.Select(r => r.LeaveTopicAsync(topic, CancellationToken.None))).ConfigureAwait(false);
                }
            });
#pragma warning restore VSTHRD101 

            // Tell routers if first time.
            if (topicHandlers.Values.Count(t => t.Topic == topic) == 1)
            {
                await Task.WhenAll(Routers.Select(r => r.JoinTopicAsync(topic, CancellationToken.None))).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///   Invoked when a router gets a message.
        /// </summary>
        /// <param name="sender">
        ///   The <see cref="IMessageRouter"/>.
        /// </param>
        /// <param name="msg">
        ///   The message.
        /// </param>
        /// <remarks>
        ///   Invokes any topic handlers and publishes the messages on the other routers.
        /// </remarks>
        void Router_MessageReceived(object sender, PublishedMessage msg)
        {
            ++MesssagesReceived;

            // Check for duplicate message.
            if (tracker.RecentlySeen(msg.MessageId))
            {
                ++DuplicateMesssagesReceived;
                return;
            }

            // Call local topic handlers.
            var handlers = topicHandlers.Values
                .Where(th => msg.Topics.Contains(th.Topic));
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Handler(msg);
                }
                catch (Exception e)
                {
                    log.Error($"Topic handler for '{handler.Topic}' failed.", e);
                }
            }

            // Tell other message routers.
            _ = Task.WhenAll(Routers
                .Where(r => r != sender)
                .Select(r => r.PublishAsync(msg, CancellationToken.None))
            );
        }

    }
}
