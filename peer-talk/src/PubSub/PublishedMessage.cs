using Ipfs;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.PubSub
{
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
        string messageId;

        /// <inheritdoc />
        public Peer Sender { get; set; }

        /// <summary>
        ///   Who sent the the message.
        /// </summary>
        public Peer Forwarder {get; set; }

        [ProtoMember(1)]
        byte[] From
        {
            get
            {
                return Sender?.Id.ToArray();
            }
            set
            {
                Sender = new Peer { Id = new MultiHash(value) };
            }
        }

        /// <inheritdoc />
        [ProtoMember(4)]
        public IEnumerable<string> Topics { get; set; }

        /// <inheritdoc />
        [ProtoMember(3)]
        public byte[] SequenceNumber { get; set; }

        /// <inheritdoc />
        [ProtoMember(2)]
        public byte[] DataBytes { get;  set; }

        /// <inheritdoc />
        public Stream DataStream
        {
            get
            {
                return new MemoryStream(DataBytes, false);
            }
        }

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
                if (messageId == null)
                {
                    messageId = Sender.Id.ToBase58() + SequenceNumber.ToHexString();
                }
                return messageId;
            }
        }

        /// <inheritdoc />
        public long Size
        {
            get { return DataBytes.Length; }
        }
    }
}
