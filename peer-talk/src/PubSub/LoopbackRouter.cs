using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.PubSub
{
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
        /// <inheritdoc />
        public event EventHandler<PublishedMessage> MessageReceived;

        /// <inheritdoc />
        public Task PublishAsync(PublishedMessage message, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            MessageReceived?.Invoke(this, message);
            return Task.CompletedTask;
        }
    }
}
