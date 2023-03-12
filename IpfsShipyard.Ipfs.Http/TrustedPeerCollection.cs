using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.Ipfs.Http
{
    /// <summary>
    ///   A list of trusted peers.
    /// </summary>
    /// <remarks>
    ///   This is the list of peers that are initially trusted by IPFS. Its equivalent to the
    ///   <see href="https://ipfs.io/ipfs/QmTkzDwWqPbnAh5YiV5VwcTLnGdwSNsNTn2aDxdXBFca7D/example#/ipfs/QmThrNbvLj7afQZhxH72m5Nn1qiVn3eMKWFYV49Zp2mv9B/bootstrap/readme.md">ipfs bootstrap</see> command.
    /// </remarks>
    /// <returns>
    ///   A series of <see cref="MultiAddress"/>.  Each address ends with an IPNS node id, for
    ///   example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ".
    /// </returns>
    public class TrustedPeerCollection : ICollection<MultiAddress>
    {
        class BootstrapListResponse
        {
            public MultiAddress[] Peers { get; set; }
        }

        IpfsClient _ipfs;
        MultiAddress[] _peers;

        internal TrustedPeerCollection(IpfsClient ipfs)
        {
            _ipfs = ipfs;
        }

        /// <inheritdoc />
        public void Add(MultiAddress peer)
        {
            if (peer == null)
                throw new ArgumentNullException();

            _ipfs.DoCommandAsync("bootstrap/add", default(CancellationToken), peer.ToString()).Wait();
            _peers = null;
        }

        /// <summary>
        ///    Add the default bootstrap nodes to the trusted peers.
        /// </summary>
        /// <remarks>
        ///    Equivalent to <c>ipfs bootstrap add --default</c>.
        /// </remarks>
        public void AddDefaultNodes()
        {
            _ipfs.DoCommandAsync("bootstrap/add", default(CancellationToken), null, "default=true").Wait();
            _peers = null;
        }

        /// <summary>
        ///    Remove all the trusted peers.
        /// </summary>
        /// <remarks>
        ///    Equivalent to <c>ipfs bootstrap rm --all</c>.
        /// </remarks>
        public void Clear()
        {
            _ipfs.DoCommandAsync("bootstrap/rm", default(CancellationToken), null, "all=true").Wait();
            _peers = null;
        }

        /// <inheritdoc />
        public bool Contains(MultiAddress item)
        {
            Fetch();
            return _peers.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(MultiAddress[] array, int index)
        {
            Fetch();
            _peers.CopyTo(array, index);
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                if (_peers == null)
                    Fetch();
                return _peers.Count();
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        ///    Remove the trusted peer.
        /// </summary>
        /// <remarks>
        ///    Equivalent to <c>ipfs bootstrap rm <i>peer</i></c>.
        /// </remarks>
        public bool Remove(MultiAddress peer)
        {
            if (peer == null)
                throw new ArgumentNullException();

            _ipfs.DoCommandAsync("bootstrap/rm", default(CancellationToken), peer.ToString()).Wait();
            _peers = null;
            return true;
        }

        /// <inheritdoc />
        public IEnumerator<MultiAddress> GetEnumerator()
        {
            Fetch();
            return ((IEnumerable<MultiAddress>)_peers).GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            Fetch();
            return _peers.GetEnumerator();
        }

        void Fetch()
        {
            _peers = _ipfs.DoCommandAsync<BootstrapListResponse>("bootstrap/list", default(CancellationToken)).Result.Peers;
        }
    }
}
