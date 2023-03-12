using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk.Protocols;
using ProtoBuf;
using Semver;

namespace IpfsShipyard.PeerTalk.PubSub;

/// <summary>
///   The original flood sub router.
/// </summary>
public class FloodRouter : IPeerProtocol, IMessageRouter
{
    private static readonly ILog log = LogManager.GetLogger(typeof(FloodRouter));

    private readonly MessageTracker tracker = new();
    private readonly ConcurrentDictionary<string, string> localTopics = new();

    /// <summary>
    ///   The topics of interest of other peers.
    /// </summary>
    public TopicManager RemoteTopics { get; set; } = new TopicManager();

    /// <inheritdoc />
    public event EventHandler<PublishedMessage> MessageReceived;

    /// <inheritdoc />
    public string Name { get; } = "floodsub";

    /// <inheritdoc />
    public SemVersion Version { get; } = new SemVersion(1, 0);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"/{Name}/{Version}";
    }

    /// <summary>
    ///   Provides access to other peers.
    /// </summary>
    public Swarm Swarm { get; set; }

    /// <inheritdoc />
    public Task StartAsync()
    {
        log.Debug("Starting");

        Swarm.AddProtocol(this);
        Swarm.ConnectionEstablished += Swarm_ConnectionEstablished;
        Swarm.PeerDisconnected += Swarm_PeerDisconnected;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        log.Debug("Stopping");

        Swarm.ConnectionEstablished -= Swarm_ConnectionEstablished;
        Swarm.PeerDisconnected -= Swarm_PeerDisconnected;
        Swarm.RemoveProtocol(this);
        RemoteTopics.Clear();
        localTopics.Clear();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ProcessMessageAsync(PeerConnection connection, Stream stream, CancellationToken cancel = default)
    {
        while (true)
        {
            var request = await ProtoBufHelper.ReadMessageAsync<PubSubMessage>(stream, cancel).ConfigureAwait(false);
            log.Debug($"got message from {connection.RemotePeer}");

            if (request.Subscriptions != null)
            {
                foreach (var sub in request.Subscriptions)
                {
                    ProcessSubscription(sub, connection.RemotePeer);
                }
            }

            if (request.PublishedMessages != null)
            {
                foreach (var msg in request.PublishedMessages)
                {
                    log.Debug($"Message for '{string.Join(", ", msg.Topics)}' fowarded by {connection.RemotePeer}");
                    msg.Forwarder = connection.RemotePeer;
                    MessageReceived?.Invoke(this, msg);
                    await PublishAsync(msg, cancel).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    ///   Process a subscription request from another peer.
    /// </summary>
    /// <param name="sub">
    ///   The subscription request.
    /// </param>
    /// <param name="remote">
    ///   The remote <see cref="Peer"/>.
    /// </param>
    /// <seealso cref="RemoteTopics"/>
    /// <remarks>
    ///   Maintains the <see cref="RemoteTopics"/>.
    /// </remarks>
    public void ProcessSubscription(Subscription sub, Peer remote)
    {
        if (sub.Subscribe)
        {
            log.Debug($"Subscribe '{sub.Topic}' by {remote}");
            RemoteTopics.AddInterest(sub.Topic, remote);
        }
        else
        {
            log.Debug($"Unsubscribe '{sub.Topic}' by {remote}");
            RemoteTopics.RemoveInterest(sub.Topic, remote);
        }
    }

    /// <inheritdoc />
    public IEnumerable<Peer> InterestedPeers(string topic)
    {
        return RemoteTopics.GetPeers(topic);
    }

    /// <inheritdoc />
    public async Task JoinTopicAsync(string topic, CancellationToken cancel)
    {
        localTopics.TryAdd(topic, topic);
        var msg = new PubSubMessage
        {
            Subscriptions = new Subscription[]
            {
                new Subscription
                {
                    Topic = topic,
                    Subscribe = true
                }
            }
        };
        try
        {
            var peers = Swarm.KnownPeers.Where(p => p.ConnectedAddress != null);
            await SendAsync(msg, peers, cancel).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            log.Warn("Join topic failed.", e);
        }
    }

    /// <inheritdoc />
    public async Task LeaveTopicAsync(string topic, CancellationToken cancel)
    {
        localTopics.TryRemove(topic, out _);
        var msg = new PubSubMessage
        {
            Subscriptions = new Subscription[]
            {
                new Subscription
                {
                    Topic = topic,
                    Subscribe = false
                }
            }
        };
        try
        {
            var peers = Swarm.KnownPeers.Where(p => p.ConnectedAddress != null);
            await SendAsync(msg, peers, cancel).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            log.Warn("Leave topic failed.", e);
        }
    }

    /// <inheritdoc />
    public Task PublishAsync(PublishedMessage message, CancellationToken cancel)
    {
        if (tracker.RecentlySeen(message.MessageId))
            return Task.CompletedTask;

        // Find a set of peers that are interested in the topic(s).
        // Exclude author and sender
        var peers = message.Topics
            .SelectMany(topic => RemoteTopics.GetPeers(topic))
            .Where(peer => peer != message.Sender && peer != message.Forwarder);

        // Forward the message.
        var forward = new PubSubMessage
        {
            PublishedMessages = new PublishedMessage[] { message }
        };

        return SendAsync(forward, peers, cancel);
    }

    private Task SendAsync(PubSubMessage msg, IEnumerable<Peer> peers, CancellationToken cancel)
    {
        // Get binary representation
        byte[] bin;
        using (var ms = new MemoryStream())
        {
            Serializer.SerializeWithLengthPrefix(ms, msg, PrefixStyle.Base128);
            bin = ms.ToArray();
        }

        return Task.WhenAll(peers.Select(p => SendAsync(bin, p, cancel)));
    }

    private async Task SendAsync(byte[] message, Peer peer, CancellationToken cancel)
    {
        try
        {
            using (var stream = await Swarm.DialAsync(peer, this.ToString(), cancel).ConfigureAwait(false))
            {
                await stream.WriteAsync(message, 0, message.Length, cancel).ConfigureAwait(false);
                await stream.FlushAsync(cancel).ConfigureAwait(false);
            }
            log.Debug($"sending message to {peer}");
            return;
        }
        catch (Exception e)
        {
            log.Debug($"{peer} refused pubsub message.", e);
        }
    }

#pragma warning disable VSTHRD100 // Avoid async void methods

    /// <summary>
    ///   Raised when a connection is established to a remote peer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="connection"></param>
    /// <remarks>
    ///   Sends the hello message to the remote peer.  The message contains
    ///   all topics that are of interest to the local peer.
    /// </remarks>
    private async void Swarm_ConnectionEstablished(object sender, PeerConnection connection)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        if (localTopics.IsEmpty)
            return;

        try
        {
            var hello = new PubSubMessage
            {
                Subscriptions = localTopics.Values
                    .Select(topic => new Subscription
                    {
                        Subscribe = true,
                        Topic = topic
                    })
                    .ToArray()
            };
            await SendAsync(hello, new Peer[] { connection.RemotePeer }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            log.Warn("Sending hello message failed", e);
        }
    }

    /// <summary>
    ///   Raised when the peer has no more connections.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="peer"></param>
    /// <remarks>
    ///   Removes the <paramref name="peer"/> from the
    ///   <see cref="RemoteTopics"/>.
    /// </remarks>
    private void Swarm_PeerDisconnected(object sender, Peer peer)
    {
        RemoteTopics.Clear(peer);
    }
}