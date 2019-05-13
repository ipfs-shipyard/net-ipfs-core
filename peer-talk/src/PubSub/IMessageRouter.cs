using Ipfs;
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
    public interface IMessageRouter : IService
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

        /// <summary>
        ///   Gets the sequence of peers interested in the topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic of interest or <b>null</b> for all topics.
        /// </param>
        /// <returns>
        ///   A sequence of <see cref="Peer"/> that are subsribed to the
        ///   <paramref name="topic"/>.
        /// </returns>
        IEnumerable<Peer> InterestedPeers(string topic);

        /// <summary>
        ///   Indicates that the local peer is interested in the topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic of interested.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task JoinTopicAsync(string topic, CancellationToken cancel);

        /// <summary>
        ///   Indicates that the local peer is no longer interested in the topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic of interested.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task LeaveTopicAsync(string topic, CancellationToken cancel);

    }
}
