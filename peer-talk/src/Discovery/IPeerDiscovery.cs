using System;
using Ipfs;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Discovery
{
    /// <summary>
    ///   Describes a service that finds a peer.
    /// </summary>
    /// <remarks>
    ///   All discovery services must raise the <see cref="PeerDiscovered"/> event.
    /// </remarks>
    public interface IPeerDiscovery : IService
    {
        /// <summary>
        ///   Raised when a peer is discovered.
        /// </summary>
        /// <remarks>
        ///   The peer must contain at least one <see cref="MultiAddress"/>.
        ///   The address must end with the ipfs protocol and the public ID
        ///   of the peer.  For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </remarks>
        event EventHandler<Peer> PeerDiscovered;
    }
}
