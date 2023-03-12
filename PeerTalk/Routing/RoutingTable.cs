using System.Collections.Generic;
using System.Linq;
using IpfsShipyard.Ipfs.Core;
using Makaretu.Collections;

namespace IpfsShipyard.PeerTalk.Routing;

internal class RoutingPeer : IContact
{
    public Peer Peer;

    public RoutingPeer(Peer peer)
    {
        Peer = peer;
    }

    public byte[] Id => RoutingTable.Key(Peer.Id);
}

/// <summary>
///   A wrapper around k-bucket, to provide easy store and retrival for peers.
/// </summary>
public class RoutingTable
{
    private readonly KBucket<RoutingPeer> _peers = new();

    /// <summary>
    ///   Creates a new instance of the <see cref="RoutingTable"/> for
    ///   the specified <see cref="Peer"/>.
    /// </summary>
    /// <param name="localPeer"></param>
    public RoutingTable(Peer localPeer)
    {
        _peers.LocalContactId = Key(localPeer.Id);
        _peers.ContactsToPing = 1;
        _peers.Ping += Peers_Ping;
    }

    /// <summary>
    ///   A k-bucket is full!
    /// </summary>
    /// <remarks>
    /// <para>
    ///  Currently this just removes the oldest contact from the list,
    ///  without acutally pinging the individual peers.
    /// </para>
    /// <para>
    ///  This is the same as go does, but should probably
    ///  be upgraded to actually ping the individual peers.
    /// </para>
    /// </remarks>
    private void Peers_Ping(object sender, PingEventArgs<RoutingPeer> e)
    {
        if (_peers.Remove(e.Oldest.First()))
        {
            _peers.Add(e.Newest);
        }
    }

    /// <summary>
    ///   Add some information about the peer.
    /// </summary>
    public void Add(Peer peer)
    {
        _peers.Add(new(peer));
    }

    /// <summary>
    ///   Remove the information about the peer.
    /// </summary>
    public void Remove(Peer peer)
    {
        _peers.Remove(new(peer));
    }

    /// <summary>
    ///   Determines in the peer exists in the routing table.
    /// </summary>
    public bool Contains(Peer peer)
    {
        return _peers.Contains(new(peer));
    }

    /// <summary>
    ///   Find the closest peers to the peer ID.
    /// </summary>
    public IEnumerable<Peer> NearestPeers(MultiHash id)
    {
        return _peers
            .Closest(Key(id))
            .Select(r => r.Peer);
    }

    /// <summary>
    ///   Converts the peer ID to a routing table key.
    /// </summary>
    /// <param name="id">A multihash</param>
    /// <returns>
    ///   The routing table key.
    /// </returns>
    /// <remarks>
    ///   The peer ID is actually a multihash, it always starts with the same characters
    ///   (ie, Qm for rsa). This causes the distribution of hashes to be
    ///   non-equally distributed across all possible hash buckets. So the re-hash
    ///   into a non-multihash is to evenly distribute the potential keys and
    ///   hash buckets.
    /// </remarks>
    /// <seealso href="https://github.com/libp2p/js-libp2p-kad-dht/issues/56#issuecomment-441378802"/>
    public static byte[] Key(MultiHash id)
    {
        return MultiHash.ComputeHash(id.ToArray(), "sha2-256").Digest;
    }
}