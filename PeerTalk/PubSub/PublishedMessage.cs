using System;
using System.Collections.Generic;
using System.IO;
using IpfsShipyard.Ipfs.Core;
using ProtoBuf;

namespace IpfsShipyard.PeerTalk.PubSub;

/// <summary>
///  A published messaged for a topic(s).
/// </summary>
/// <seealso ref="https://github.com/libp2p/specs/blob/master/pubsub/README.md"/>
/// <remarks>
///   TODO: Sender should really be called Author.
///
/// </remarks>
[ProtoContract]
public class PublishedMessage : IPublishedMessage
{
    private string _messageId;

    /// <inheritdoc />
    public Peer Sender { get; set; }

    /// <summary>
    ///   Who sent the the message.
    /// </summary>
    public Peer Forwarder { get; set; }

    [ProtoMember(1)]
    private byte[] From
    {
        get => Sender?.Id.ToArray();
        set => Sender = new Peer { Id = new MultiHash(value) };
    }

    /// <inheritdoc />
    [ProtoMember(4)]
    public IEnumerable<string> Topics { get; set; }

    /// <inheritdoc />
    [ProtoMember(3)]
    public byte[] SequenceNumber { get; set; }

    /// <inheritdoc />
    [ProtoMember(2)]
    public byte[] DataBytes { get; set; }

    /// <inheritdoc />
    public Stream DataStream => new MemoryStream(DataBytes, false);

    /// <summary>>
    ///   NOT SUPPORTED, use <see cref="MessageId"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">
    ///   A published message does not have a content id.
    /// </exception>
    public Cid Id => throw new NotSupportedException();

    /// <summary>
    ///   A universally unique id for the message.
    /// </summary>
    /// <value>
    ///   The sender's ID concatenated with the <see cref="SequenceNumber"/>.
    /// </value>
    public string MessageId
    {
        get
        {
            _messageId ??= Sender.Id.ToBase58() + SequenceNumber.ToHexString();
            return _messageId;
        }
    }

    /// <inheritdoc />
    public long Size => DataBytes.Length;
}