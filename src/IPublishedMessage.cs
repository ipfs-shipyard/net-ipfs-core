using System.Collections.Generic;
using Ipfs.CoreApi;

namespace Ipfs
{
    /// <summary>
    ///   A published message.
    /// </summary>
    /// <remarks>
    ///   The <see cref="IPubSubApi"/> is used to publish and subsribe to a message.
    ///   <para/>
    ///   Interface layout sourced from Kubo API: <see href="https://github.com/ipfs/kubo/blob/f5b855550ca73acf5dd3a2001e2a7192cab7c249/core/commands/pubsub.go#L111"/>.
    /// </remarks>
    public interface IPublishedMessage
    {
        /// <summary>
        ///   Contents as a byte array.
        /// </summary>
        /// <remarks>
        ///   It is never <b>null</b>.
        /// </remarks>
        /// <value>
        ///   The contents as a sequence of bytes.
        /// </value>
        byte[] DataBytes { get; }

        /// <summary>
        ///   The sender of the message.
        /// </summary>
        /// <value>
        ///   The peer that sent the message.
        /// </value>
        Peer Sender { get; }

        /// <summary>
        ///   The topics of the message.
        /// </summary>
        /// <value>
        ///   All topics related to this message.
        /// </value>
        IEnumerable<string> Topics { get; }

        /// <summary>
        ///   The sequence number of the message.
        /// </summary>
        /// <value>
        ///   A sender unique id for the message.
        /// </value>
        byte[] SequenceNumber { get; }
    }
}
