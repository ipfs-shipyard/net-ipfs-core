using System;
using System.Linq;
using System.Threading;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.PeerTalk;

/// <summary>
///   Maintains a minimum number of peer connections.
/// </summary>
/// <remarks>
///   Listens to the <see cref="Swarm"/> and automatically dials a
///   new <see cref="Peer"/> when required.
/// </remarks>
public class AutoDialer : IDisposable
{
    private static readonly ILog log = LogManager.GetLogger(typeof(AutoDialer));

    /// <summary>
    ///   The default minimum number of connections to maintain (16).
    /// </summary>
    public const int DefaultMinConnections = 8;

    /// <summary>
    ///   The default maximum number of connections to maintain (1024).
    /// </summary>
    public const int DefaultMaxConnections = 21;

    private readonly Swarm _swarm;
    private int _pendingConnects;

    /// <summary>
    ///   Creates a new instance of the <see cref="AutoDialer"/> class.
    /// </summary>
    /// <param name="swarm">
    ///   Provides access to other peers.
    /// </param>
    public AutoDialer(Swarm swarm)
    {
        _swarm = swarm;
        swarm.PeerDiscovered += OnPeerDiscovered;
        swarm.PeerDisconnected += OnPeerDisconnected;
    }

    /// <summary>
    ///  Releases the unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing">
    ///   <b>true</b> to release both managed and unmanaged resources; <b>false</b>
    ///   to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _swarm.PeerDiscovered -= OnPeerDiscovered;
            _swarm.PeerDisconnected -= OnPeerDisconnected;
        }
    }

    /// <summary>
    ///   Performs application-defined tasks associated with freeing,
    ///   releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() => Dispose(true);

    /// <summary>
    ///   The low water mark for peer connections.
    /// </summary>
    /// <value>
    ///   Defaults to <see cref="DefaultMinConnections"/>.
    /// </value>
    /// <remarks>
    ///   Setting this to zero will basically disable the auto dial features.
    /// </remarks>
    public int MinConnections { get; set; } = DefaultMinConnections;

    /// <summary>
    ///   The higher water mark for peer connections.
    /// </summary>
    /// <value>
    ///   Defaults to <see cref="DefaultMinConnections"/>.
    /// </value>
    /// <remarks>
    ///   Setting this to zero will basically disable the auto dial features.
    /// </remarks>
    public int MaxConnections { get; set; } = DefaultMaxConnections;

#pragma warning disable VSTHRD100 // Avoid async void methods

    /// <summary>
    ///   Called when the swarm has a new peer.
    /// </summary>
    /// <param name="sender">
    ///   The swarm of peers.
    /// </param>
    /// <param name="peer">
    ///   The peer that was discovered.
    /// </param>
    /// <remarks>
    ///   If the <see cref="MinConnections"/> is not reached, then the
    ///   <paramref name="peer"/> is dialed.
    /// </remarks>
    private async void OnPeerDiscovered(object sender, Peer peer)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        var n = _swarm.Manager.Connections.Count() + _pendingConnects;
        if (_swarm.IsRunning && n < MinConnections)
        {
            Interlocked.Increment(ref _pendingConnects);
            log.Debug($"Dialing new {peer}");
            try
            {
                await _swarm.ConnectAsync(peer).ConfigureAwait(false);
            }
            catch (Exception)
            {
                log.Warn($"Failed to dial {peer}");
            }
            finally
            {
                Interlocked.Decrement(ref _pendingConnects);
            }
        }
    }

#pragma warning disable VSTHRD100 // Avoid async void methods

    /// <summary>
    ///   Called when the swarm has lost a connection to a peer.
    /// </summary>
    /// <param name="sender">
    ///   The swarm of peers.
    /// </param>
    /// <param name="disconnectedPeer">
    ///   The peer that was disconnected.
    /// </param>
    /// <remarks>
    ///   If the <see cref="MinConnections"/> is not reached, then another
    ///   peer is dialed.
    /// </remarks>
    private async void OnPeerDisconnected(object sender, Peer disconnectedPeer)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        var n = _swarm.Manager.Connections.Count() + _pendingConnects;

        if (MaxConnections > 0) // use range
        {
            // check range
            if (!_swarm.IsRunning || (n > MinConnections && n <= MaxConnections))
                return;
        }
        else // use minimum
        {
            //
            if (!_swarm.IsRunning || n >= MinConnections)
                return;
        }

        // Find a random peer to connect with.
        var peers = _swarm.KnownPeers
            .Where(p => p.ConnectedAddress == null && p != disconnectedPeer && _swarm.IsAllowed(p) && !_swarm.HasPendingConnection(p))
            .ToArray();
        if (peers.Length == 0)
            return;
        var rng = new Random();
        var peer = peers[rng.Next(peers.Length)];

        Interlocked.Increment(ref _pendingConnects);
        log.Debug($"Dialing {peer}");
        try
        {
            await _swarm.ConnectAsync(peer).ConfigureAwait(false);
        }
        catch (Exception)
        {
            log.Warn($"Failed to dial {peer}");
        }
        finally
        {
            Interlocked.Decrement(ref _pendingConnects);
        }
    }
}