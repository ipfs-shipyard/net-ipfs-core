using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using IpfsShipyard.PeerTalk.Protocols;
using ProtoBuf;
using Semver;

namespace IpfsShipyard.PeerTalk.Routing;

/// <summary>
///   DHT Protocol version 1.0
/// </summary>
public class Dht1 : IPeerProtocol, IService, IPeerRouting, IContentRouting
{
    private static readonly ILog log = LogManager.GetLogger(typeof(Dht1));

    /// <inheritdoc />
    public string Name { get; } = "ipfs/kad";

    /// <inheritdoc />
    public SemVersion Version { get; } = new(1, 0);

    /// <summary>
    ///   Provides access to other peers.
    /// </summary>
    public Swarm Swarm { get; set; }

    /// <summary>
    ///  Routing information on peers.
    /// </summary>
    public RoutingTable RoutingTable;

    /// <summary>
    ///   Peers that can provide some content.
    /// </summary>
    public ContentRouter ContentRouter;

    /// <summary>
    ///   The number of closer peers to return.
    /// </summary>
    /// <value>
    ///   Defaults to 20.
    /// </value>
    public int CloserPeerCount { get; set; } = 20;

    /// <summary>
    ///   Raised when the DHT is stopped.
    /// </summary>
    /// <seealso cref="StopAsync"/>
    public event EventHandler Stopped;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"/{Name}/{Version}";
    }

    /// <inheritdoc />
    public async Task ProcessMessageAsync(PeerConnection connection, Stream stream, CancellationToken cancel = default)
    {
        while (true)
        {
            var request = await ProtoBufHelper.ReadMessageAsync<DhtMessage>(stream, cancel).ConfigureAwait(false);

            log.Debug($"got {request.Type} from {connection.RemotePeer}");
            var response = new DhtMessage
            {
                Type = request.Type,
                ClusterLevelRaw = request.ClusterLevelRaw
            };
            switch (request.Type)
            {
                case MessageType.Ping:
                    response = ProcessPing(request, response);
                    break;

                case MessageType.FindNode:
                    response = ProcessFindNode(request, response);
                    break;

                case MessageType.GetProviders:
                    response = ProcessGetProviders(request, response);
                    break;

                case MessageType.AddProvider:
                    response = ProcessAddProvider(connection.RemotePeer, request, response);
                    break;

                default:
                    log.Debug($"unknown {request.Type} from {connection.RemotePeer}");
                    // TODO: Should we close the stream?
                    continue;
            }
            if (response != null)
            {
                Serializer.SerializeWithLengthPrefix(stream, response, PrefixStyle.Base128);
                await stream.FlushAsync(cancel).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public Task StartAsync()
    {
        log.Debug("Starting");

        RoutingTable = new(Swarm.LocalPeer);
        ContentRouter = new();
        Swarm.AddProtocol(this);
        Swarm.PeerDiscovered += Swarm_PeerDiscovered;
        Swarm.PeerRemoved += Swarm_PeerRemoved;
        foreach (var peer in Swarm.KnownPeers)
        {
            RoutingTable.Add(peer);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        log.Debug("Stopping");

        Swarm.RemoveProtocol(this);
        Swarm.PeerDiscovered -= Swarm_PeerDiscovered;
        Swarm.PeerRemoved -= Swarm_PeerRemoved;

        Stopped?.Invoke(this, EventArgs.Empty);
        ContentRouter?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    ///   The swarm has discovered a new peer, update the routing table.
    /// </summary>
    private void Swarm_PeerDiscovered(object sender, Peer e)
    {
        RoutingTable.Add(e);
    }

    /// <summary>
    ///   The swarm has removed a peer, update the routing table.
    /// </summary>
    private void Swarm_PeerRemoved(object sender, Peer e)
    {
        RoutingTable.Remove(e);
    }

    /// <inheritdoc />
    public async Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default)
    {
        // Can always find self.
        if (Swarm.LocalPeer.Id == id)
            return Swarm.LocalPeer;

        // Maybe the swarm knows about it.
        var found = Swarm.KnownPeers.FirstOrDefault(p => p.Id == id);
        if (found?.Addresses.Count() > 0)
            return found;

        // Ask our peers for information on the requested peer.
        var dquery = new DistributedQuery<Peer>
        {
            QueryType = MessageType.FindNode,
            QueryKey = id,
            Dht = this,
            AnswersNeeded = 1
        };
        await dquery.RunAsync(cancel).ConfigureAwait(false);

        // If not found, return the closest peer.
        if (!dquery.Answers.Any())
        {
            return RoutingTable.NearestPeers(id).FirstOrDefault();
        }

        return dquery.Answers.First();
    }

    /// <inheritdoc />
    public Task ProvideAsync(Cid cid, bool advertise = true, CancellationToken cancel = default)
    {
        ContentRouter.Add(cid, Swarm.LocalPeer.Id);
        if (advertise)
        {
            Advertise(cid);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Peer>> FindProvidersAsync(
        Cid id,
        int limit = 20,
        Action<Peer> action = null,
        CancellationToken cancel = default)
    {
        var dquery = new DistributedQuery<Peer>
        {
            QueryType = MessageType.GetProviders,
            QueryKey = id.Hash,
            Dht = this,
            AnswersNeeded = limit,
        };
        if (action != null)
        {
            dquery.AnswerObtained += (_, e) => action.Invoke(e);
        }

        // Add any providers that we already know about.
        var providers = ContentRouter
            .Get(id)
            .Select(pid =>
            {
                return (pid == Swarm.LocalPeer.Id)
                    ? Swarm.LocalPeer
                    : Swarm.RegisterPeer(new() { Id = pid });
            });
        foreach (var provider in providers)
        {
            dquery.AddAnswer(provider);
        }

        // Ask our peers for more providers.
        if (limit > dquery.Answers.Count())
        {
            await dquery.RunAsync(cancel).ConfigureAwait(false);
        }

        return dquery.Answers.Take(limit);
    }

    /// <summary>
    ///   Advertise that we can provide the CID to the X closest peers
    ///   of the CID.
    /// </summary>
    /// <param name="cid">
    ///   The CID to advertise.
    /// </param>
    /// <remarks>
    ///   This starts a background process to send the AddProvider message
    ///   to the 4 closest peers to the <paramref name="cid"/>.
    /// </remarks>
    public void Advertise(Cid cid)
    {
        _ = Task.Run(async () =>
        {
            var advertsNeeded = 4;
            var message = new DhtMessage
            {
                Type = MessageType.AddProvider,
                Key = cid.Hash.ToArray(),
                ProviderPeers = new DhtPeerMessage[]
                {
                    new()
                    {
                        Id = Swarm.LocalPeer.Id.ToArray(),
                        Addresses = Swarm.LocalPeer.Addresses
                            .Select(a => a.WithoutPeerId().ToArray())
                            .ToArray()
                    }
                }
            };
            var peers = RoutingTable
                .NearestPeers(cid.Hash)
                .Where(p => p != Swarm.LocalPeer);
            foreach (var peer in peers)
            {
                try
                {
                    using (var stream = await Swarm.DialAsync(peer, ToString()))
                    {
                        Serializer.SerializeWithLengthPrefix(stream, message, PrefixStyle.Base128);
                        await stream.FlushAsync();
                    }
                    if (--advertsNeeded == 0)
                        break;
                }
                catch
                {
                    // eat it.  This is fire and forget.
                }
            }
        });
    }

    /// <summary>
    ///   Process a ping request.
    /// </summary>
    /// <remarks>
    ///   Simply return the <paramref name="request"/>.
    /// </remarks>
    private DhtMessage ProcessPing(DhtMessage request, DhtMessage response)
    {
        return request;
    }

    /// <summary>
    ///   Process a find node request.
    /// </summary>
    public DhtMessage ProcessFindNode(DhtMessage request, DhtMessage response)
    {
        // Some random walkers generate a random Key that is not hashed.
        MultiHash peerId;
        try
        {
            peerId = new(request.Key);
        }
        catch (Exception)
        {
            log.Error($"Bad FindNode request key {request.Key.ToHexString()}");
            peerId = MultiHash.ComputeHash(request.Key);
        }

        // Do we know the peer?.
        var found = Swarm.LocalPeer.Id == peerId ? Swarm.LocalPeer : Swarm.KnownPeers.FirstOrDefault(p => p.Id == peerId);

        // Find the closer peers.
        var closerPeers = new List<Peer>();
        if (found != null)
        {
            closerPeers.Add(found);
        }
        else
        {
            closerPeers.AddRange(RoutingTable.NearestPeers(peerId).Take(CloserPeerCount));
        }

        // Build the response.
        response.CloserPeers = closerPeers
            .Select(peer => new DhtPeerMessage
            {
                Id = peer.Id.ToArray(),
                Addresses = peer.Addresses.Select(a => a.WithoutPeerId().ToArray()).ToArray()
            })
            .ToArray();

        if (log.IsDebugEnabled)
            log.Debug($"returning {response.CloserPeers.Length} closer peers");
        return response;
    }

    /// <summary>
    ///   Process a get provider request.
    /// </summary>
    public DhtMessage ProcessGetProviders(DhtMessage request, DhtMessage response)
    {
        // Find providers for the content.
        var cid = new Cid { Hash = new(request.Key) };
        response.ProviderPeers = ContentRouter
            .Get(cid)
            .Select(pid =>
            {
                var peer = (pid == Swarm.LocalPeer.Id)
                    ? Swarm.LocalPeer
                    : Swarm.RegisterPeer(new() { Id = pid });
                return new DhtPeerMessage
                {
                    Id = peer.Id.ToArray(),
                    Addresses = peer.Addresses.Select(a => a.WithoutPeerId().ToArray()).ToArray()
                };
            })
            .Take(21)
            .ToArray();

        // Also return the closest peers
        return ProcessFindNode(request, response);
    }

    /// <summary>
    ///   Process an add provider request.
    /// </summary>
    public DhtMessage ProcessAddProvider(Peer remotePeer, DhtMessage request, DhtMessage response)
    {
        if (request.ProviderPeers == null)
        {
            return null;
        }
        Cid cid;
        try
        {
            cid = new() { Hash = new(request.Key) };
        }
        catch (Exception)
        {
            log.Error($"Bad AddProvider request key {request.Key.ToHexString()}");
            return null;
        }
        var providers = request.ProviderPeers
            .Select(p => p.TryToPeer(out var peer) ? peer : (Peer)null)
            .Where(p => p != null && p == remotePeer && p.Addresses.Any() && Swarm.IsAllowed(p));
        foreach (var provider in providers)
        {
            Swarm.RegisterPeer(provider);
            ContentRouter.Add(cid, provider.Id);
        }

        // There is no response for this request.
        return null;
    }
}