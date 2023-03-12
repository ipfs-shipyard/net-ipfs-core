using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.PeerTalk.PubSub;

/// <summary>
///   A message router that always raises <see cref="MessageReceived"/>
///   when a message is published.
/// </summary>
/// <remarks>
///   The allows the <see cref="NotificationService"/> to invoke the
///   local subscribtion handlers.
/// </remarks>
public class LoopbackRouter : IMessageRouter
{
    private readonly MessageTracker tracker = new();

    /// <inheritdoc />
    public event EventHandler<PublishedMessage> MessageReceived;

    /// <inheritdoc />
    public IEnumerable<Peer> InterestedPeers(string topic)
    {
        return Enumerable.Empty<Peer>();
    }

    /// <inheritdoc />
    public Task JoinTopicAsync(string topic, CancellationToken cancel)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LeaveTopicAsync(string topic, CancellationToken cancel)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PublishAsync(PublishedMessage message, CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();

        if (!tracker.RecentlySeen(message.MessageId))
        {
            MessageReceived?.Invoke(this, message);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}