using System.Linq;
using IpfsShipyard.Ipfs.Core;
using ProtoBuf;

namespace IpfsShipyard.PeerTalk.Routing;
// From https://github.com/libp2p/js-libp2p-kad-dht/blob/master/src/message/dht.proto.js\
// and https://github.com/libp2p/go-libp2p-kad-dht/blob/master/pb/dht.proto

/// <summary>
///   TODO
/// </summary>
[ProtoContract]
public class DhtRecordMessage
{
    /// <summary>
    ///   TODO
    /// </summary>
    [ProtoMember(1)]
    public byte[] Key { get; set; }

    /// <summary>
    ///   TODO
    /// </summary>
    [ProtoMember(2)]
    public byte[] Value { get; set; }

    /// <summary>
    ///   TODO
    /// </summary>
    [ProtoMember(3)]
    public byte[] Author { get; set; }

    /// <summary>
    ///   TODO
    /// </summary>
    [ProtoMember(4)]
    public byte[] Signature { get; set; }

    /// <summary>
    ///   TODO
    /// </summary>
    [ProtoMember(5)]
    public string TimeReceived { get; set; }
}

/// <summary>
///   The type of DHT/KAD message.
/// </summary>
public enum MessageType
{
    /// <summary>
    ///   Put a value.
    /// </summary>
    PutValue = 0,

    /// <summary>
    ///   Get a value.
    /// </summary>
    GetValue = 1,

    /// <summary>
    ///   Indicate that a peer can provide something.
    /// </summary>
    AddProvider = 2,

    /// <summary>
    ///   Get the providers for something.
    /// </summary>
    GetProviders = 3,

    /// <summary>
    ///   Find a peer.
    /// </summary>
    FindNode = 4,

    /// <summary>
    ///   NYI
    /// </summary>
    Ping = 5
}

/// <summary>
///   The connection status.
/// </summary>
public enum ConnectionType
{
    /// <summary>
    /// Sender does not have a connection to peer, and no extra information (default)
    /// </summary>
    NotConnected = 0,

    /// <summary>
    /// Sender has a live connection to peer
    /// </summary>
    Connected = 1,

    /// <summary>
    /// Sender recently connected to peer
    /// </summary>
    CanConnect = 2,

    /// <summary>
    /// Sender recently tried to connect to peer repeatedly but failed to connect
    /// ("try" here is loose, but this should signal "made strong effort, failed")
    /// </summary>
    CannotConnect = 3
}

/// <summary>
///   Information about a peer.
/// </summary>
[ProtoContract]
public class DhtPeerMessage
{
    /// <summary>
    /// ID of a given peer.
    /// </summary>
    /// <value>
    ///   The <see cref="MultiHash"/> as a byte array,
    /// </value>
    [ProtoMember(1)]
    public byte[] Id { get; set; }

    /// <summary>
    /// Addresses for a given peer
    /// </summary>
    /// <value>
    ///   A sequence of <see cref="MultiAddress"/> as a byte array.
    /// </value>
    [ProtoMember(2)]
    public byte[][] Addresses { get; set; }

    /// <summary>
    /// used to signal the sender's connection capabilities to the peer
    /// </summary>
    [ProtoMember(3)]
    public ConnectionType Connection { get; set; }

    /// <summary>
    ///   Convert the message into a <see cref="Peer"/>.
    /// </summary>
    /// <param name="peer"></param>
    /// <returns></returns>
    public bool TryToPeer(out Peer peer)
    {
        peer = null;

        // Sanity checks.
        if (Id == null || Id.Length == 0)
            return false;

        var id = new MultiHash(Id);
        peer = new()
        {
            Id = id
        };
        if (Addresses != null)
        {
            var x = new MultiAddress($"/ipfs/{id}");
            peer.Addresses = Addresses
                .Select(bytes =>
                {
                    try
                    {
                        var ma = new MultiAddress(bytes);
                        ma.Protocols.AddRange(x.Protocols);
                        return ma;
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(a => a != null)
                .ToArray();
        }

        return true;
    }
}

/// <summary>
///   The DHT message exchanged between peers.
/// </summary>
[ProtoContract]
public class DhtMessage
{
    /// <summary>
    /// What type of message it is.
    /// </summary>
    [ProtoMember(1)]
    public MessageType Type { get; set; }

    /// <summary>
    ///   Coral cluster level.
    /// </summary>
    [ProtoMember(10)]
    public int ClusterLevelRaw { get; set; }

    /// <summary>
    ///   TODO
    /// </summary>
    [ProtoMember(2)]
    public byte[] Key { get; set; }

    /// <summary>
    ///   TODO
    /// </summary>
    [ProtoMember(3)]
    public DhtRecordMessage Record { get; set; }

    /// <summary>
    ///   The closer peers for a query.
    /// </summary>
    [ProtoMember(8)]
    public DhtPeerMessage[] CloserPeers { get; set; }

    /// <summary>
    ///  The providers for a query.
    /// </summary>
    [ProtoMember(9)]
    public DhtPeerMessage[] ProviderPeers { get; set; }
}