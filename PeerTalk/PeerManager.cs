using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.PeerTalk;

/// <summary>
///   Manages the peers.
/// </summary>
/// <remarks>
///    Listens to the <see cref="Swarm"/> events to determine the state
///    of a peer.
/// </remarks>
public class PeerManager : IService
{
    private static readonly ILog log = LogManager.GetLogger(typeof(PeerManager));
    private CancellationTokenSource cancel;

    /// <summary>
    ///   Initial time to wait before attempting a reconnection
    ///   to a dead peer.
    /// </summary>
    /// <value>
    ///   Defaults to 1 minute.
    /// </value>
    public TimeSpan InitialBackoff = TimeSpan.FromMinutes(1);

    /// <summary>
    ///   When reached, the peer is considered permanently dead.
    /// </summary>
    /// <value>
    ///   Defaults to 64 minutes.
    /// </value>
    public TimeSpan MaxBackoff = TimeSpan.FromMinutes(64);

    /// <summary>
    ///   Provides access to other peers.
    /// </summary>
    public Swarm Swarm { get; set; }

    /// <summary>
    ///   The peers that are reachable.
    /// </summary>
    public ConcurrentDictionary<Peer, DeadPeer> DeadPeers = new();

    /// <inheritdoc />
    public Task StartAsync()
    {
        Swarm.ConnectionEstablished += Swarm_ConnectionEstablished;
        Swarm.PeerNotReachable += Swarm_PeerNotReachable;

        cancel = new CancellationTokenSource();
        var _ = PhoenixAsync(cancel.Token);

        log.Debug("started");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        Swarm.ConnectionEstablished -= Swarm_ConnectionEstablished;
        Swarm.PeerNotReachable -= Swarm_PeerNotReachable;
        DeadPeers.Clear();

        cancel.Cancel();
        cancel.Dispose();

        log.Debug("stopped");
        return Task.CompletedTask;
    }

    /// <summary>
    ///   Indicates that the peer can not be connected to.
    /// </summary>
    /// <param name="peer"></param>
    public void SetNotReachable(Peer peer)
    {
        var dead = DeadPeers.AddOrUpdate(peer,
            new DeadPeer
            {
                Peer = peer,
                Backoff = InitialBackoff,
                NextAttempt = DateTime.Now + InitialBackoff
            },
            (_, existing) =>
            {
                existing.Backoff += existing.Backoff;
                existing.NextAttempt = existing.Backoff <= MaxBackoff
                    ? DateTime.Now + existing.Backoff
                    : DateTime.MaxValue;
                return existing;
            });

        Swarm.BlackList.Add($"/p2p/{peer.Id}");
        if (dead.NextAttempt != DateTime.MaxValue)
        {
            log.DebugFormat("Dead '{0}' for {1} minutes.", dead.Peer, dead.Backoff.TotalMinutes);
        }
        else
        {
            Swarm.DeregisterPeer(dead.Peer);
            log.DebugFormat("Permanently dead '{0}'.", dead.Peer);
        }
    }

    /// <summary>
    ///   Indicates that the peer can be connected to.
    /// </summary>
    /// <param name="peer"></param>
    public void SetReachable(Peer peer)
    {
        log.DebugFormat("Alive '{0}'.", peer);

        DeadPeers.TryRemove(peer, out DeadPeer _);
        Swarm.BlackList.Remove($"/p2p/{peer.Id}");
    }

    /// <summary>
    ///   Is invoked by the <see cref="Swarm"/> when a peer can not be connected to.
    /// </summary>
    private void Swarm_PeerNotReachable(object sender, Peer peer)
    {
        SetNotReachable(peer);
    }

    /// <summary>
    ///   Is invoked by the <see cref="Swarm"/> when a peer is connected to.
    /// </summary>
    private void Swarm_ConnectionEstablished(object sender, PeerConnection connection)
    {
        SetReachable(connection.RemotePeer);
    }

    /// <summary>
    ///   Background process to try reconnecting to a dead peer.
    /// </summary>
    private async Task PhoenixAsync(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(InitialBackoff);
                var now = DateTime.Now;
                await DeadPeers.Values
                    .Where(p => p.NextAttempt < now)
                    .ParallelForEachAsync(async dead =>
                    {
                        log.DebugFormat("Attempt reconnect to {0}", dead.Peer);
                        Swarm.BlackList.Remove($"/p2p/{dead.Peer.Id}");
                        try
                        {
                            await Swarm.ConnectAsync(dead.Peer, cancel.Token);
                        }
                        catch
                        {
                            // eat it
                        }
                    }, maxDoP: 10);
            }
            catch
            {
                // eat it.
            }
        }
    }
}

/// <summary>
///   Information on a peer that is not reachable.
/// </summary>
public class DeadPeer
{
    /// <summary>
    ///   The peer that does not respond.
    /// </summary>
    public Peer Peer { get; set; }

    /// <summary>
    ///   How long to wait before attempting another connect.
    /// </summary>
    public TimeSpan Backoff { get; set; }

    /// <summary>
    ///   When another connect should be tried.
    /// </summary>
    public DateTime NextAttempt { get; set; }
}