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
    ///   The PubSub message exchanged between peers.
    /// </summary>
    /// <seealso ref="https://github.com/libp2p/specs/blob/master/pubsub/README.md"/>
    [ProtoContract]
    public class PubSubMessage
    {
        /// <summary>
        ///   Sequence of topic subscriptions of the sender.
        /// </summary>
        [ProtoMember(1)]
        public Subscription[] Subscriptions;

        /// <summary>
        ///   Sequence of topic messages.
        /// </summary>
        [ProtoMember(2)]
        public PublishedMessage[] PublishedMessages;
    }

    /// <summary>
    ///   A peer's subscription to a topic.
    /// </summary>
    /// <seealso ref="https://github.com/libp2p/specs/blob/master/pubsub/README.md"/>
    [ProtoContract]
    public class Subscription
    {
        /// <summary>
        ///   Determines if the topic is subscribed to.
        /// </summary>
        /// <value>
        ///   <b>true</b> if subscribing; otherwise, <b>false</b> if
        ///   unsubscribing.
        /// </value>
        [ProtoMember(1)]
        public bool Subscribe;

        /// <summary>
        ///   The topic name/id.
        /// </summary>
        [ProtoMember(2)]
        public string Topic;
    }

}
