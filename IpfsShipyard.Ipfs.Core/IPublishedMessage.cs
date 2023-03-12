using System.Collections.Generic;
using IpfsShipyard.Ipfs.Core.CoreApi;

namespace IpfsShipyard.Ipfs.Core;

/// <summary>
///   A published message.
/// </summary>
/// <remarks>
///   The <see cref="IPubSubApi"/> is used to publish and subscribe to a message.
/// </remarks>
public interface IPublishedMessage : IDataBlock
{
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