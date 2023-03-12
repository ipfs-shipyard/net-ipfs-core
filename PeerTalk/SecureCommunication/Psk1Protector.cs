using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.PeerTalk.Cryptography;

namespace IpfsShipyard.PeerTalk.SecureCommunication;

/// <summary>
///   Provides access to a private network of peers that
///   uses a <see cref="PreSharedKey"/>.
/// </summary>
/// <remarks>
///   The <see cref="Swarm"/> calls the network protector whenever a connection
///   is being established with another peer.
/// </remarks>
/// <seealso href="https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md"/>
public class Psk1Protector : INetworkProtector
{
    /// <summary>
    ///   The key of the private network.
    /// </summary>
    /// <value>
    ///   Only peers with this key can be communicated with.
    /// </value>
    public PreSharedKey Key { private get; set;}

    /// <inheritdoc />
    public Task<Stream> ProtectAsync(PeerConnection connection, CancellationToken cancel = default)
    {
        return Task.FromResult<Stream>(new Psk1Stream(connection.Stream, Key));
    }
}