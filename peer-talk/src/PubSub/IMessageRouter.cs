using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.PubSub
{
    /// <summary>
    ///   Routes pub/sub messages to other peers.
    /// </summary>
    public interface IMessageRouter
    {
        /// <summary>
        ///   Sends the message to other peers.
        /// </summary>
        /// <param name="message">
        ///   The message to send.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task PublishAsync(PublishedMessage message, CancellationToken cancel);

        /// <summary>
        ///   Raised when a new message is received.
        /// </summary>
        event EventHandler<PublishedMessage> MessageReceived;
    }
}
