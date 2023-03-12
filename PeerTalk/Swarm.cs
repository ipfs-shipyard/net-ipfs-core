using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using IpfsShipyard.PeerTalk.Cryptography;
using IpfsShipyard.PeerTalk.Protocols;
using IpfsShipyard.PeerTalk.Transports;
using Nito.AsyncEx;

namespace IpfsShipyard.PeerTalk;

/// <summary>
///   Manages communication with other peers.
/// </summary>
public class Swarm : IService, IPolicy<MultiAddress>, IPolicy<Peer>
{
    private static readonly ILog log = LogManager.GetLogger(typeof(Swarm));

    /// <summary>
    ///   The time to wait for a low level connection to be established.
    /// </summary>
    /// <value>
    ///   Defaults to 33 seconds.
    /// </value>
    public TimeSpan TransportConnectionTimeout = TimeSpan.FromSeconds(33);

    /// <summary>
    ///  The supported protocols.
    /// </summary>
    /// <remarks>
    ///   Use sychronized access, e.g. <c>lock (protocols) { ... }</c>.
    /// </remarks>
    private List<IPeerProtocol> _protocols = new()
    {
        new Multistream1(),
        new SecureCommunication.Secio1(),
        new Identify1(),
        new Mplex67()
    };

    /// <summary>
    ///   Added to connection protocols when needed.
    /// </summary>
    private readonly Plaintext1 _plaintext1 = new();

    private Peer _localPeer;

    /// <summary>
    ///   Raised when a listener is establihed.
    /// </summary>
    /// <remarks>
    ///   Raised when <see cref="StartListeningAsync(MultiAddress)"/>
    ///   succeeds.
    /// </remarks>
    public event EventHandler<Peer> ListenerEstablished;

    /// <summary>
    ///   Raised when a connection to another peer is established.
    /// </summary>
    public event EventHandler<PeerConnection> ConnectionEstablished;

    /// <summary>
    ///   Raised when a new peer is discovered for the first time.
    /// </summary>
    public event EventHandler<Peer> PeerDiscovered;

    /// <summary>
    ///   Raised when a peer's connection is closed.
    /// </summary>
    public event EventHandler<Peer> PeerDisconnected;

    /// <summary>
    ///   Raised when a peer should no longer be used.
    /// </summary>
    /// <remarks>
    ///   This event indicates that the peer has been removed
    ///   from the <see cref="KnownPeers"/> and should no longer
    ///   be used.
    /// </remarks>
    public event EventHandler<Peer> PeerRemoved;

    /// <summary>
    ///   Raised when a peer cannot be connected to.
    /// </summary>
    public event EventHandler<Peer> PeerNotReachable;

    /// <summary>
    ///  The local peer.
    /// </summary>
    /// <value>
    ///   The local peer must have an <see cref="Peer.Id"/> and
    ///   <see cref="Peer.PublicKey"/>.
    /// </value>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public Peer LocalPeer
    {
        get { return _localPeer; }
        set
        {
            if (value == null)
                throw new ArgumentNullException();
            if (value.Id == null)
                throw new ArgumentNullException("peer.Id");
            if (value.PublicKey == null)
                throw new ArgumentNullException("peer.PublicKey");
            if (!value.IsValid())
                throw new ArgumentException("Invalid peer.");
            _localPeer = value;
        }
    }

    /// <summary>
    ///   The private key of the local peer.
    /// </summary>
    /// <value>
    ///   Used to prove the identity of the <see cref="LocalPeer"/>.
    /// </value>
    public Key LocalPeerKey { get; set; }

    /// <summary>
    ///   Other nodes. Key is the bae58 hash of the peer ID.
    /// </summary>
    private readonly ConcurrentDictionary<string, Peer> _otherPeers = new();

    /// <summary>
    ///   Used to cancel any task when the swarm is stopped.
    /// </summary>
    private CancellationTokenSource _swarmCancellation;

    /// <summary>
    ///  Outstanding connection tasks initiated by the local peer.
    /// </summary>
    private readonly ConcurrentDictionary<Peer, AsyncLazy<PeerConnection>> _pendingConnections = new();

    /// <summary>
    ///  Outstanding connection tasks initiated by a remote peer.
    /// </summary>
    private readonly ConcurrentDictionary<MultiAddress, object> _pendingRemoteConnections = new();

    /// <summary>
    ///   Manages the swarm's peer connections.
    /// </summary>
    public ConnectionManager Manager = new ConnectionManager();

    /// <summary>
    ///   Use to find addresses of a peer.
    /// </summary>
    public IPeerRouting Router { get; set; }

    /// <summary>
    ///   Provides access to a private network of peers.
    /// </summary>
    public INetworkProtector NetworkProtector { get; set; }

    /// <summary>
    ///   Determines if the swarm has been started.
    /// </summary>
    /// <value>
    ///   <b>true</b> if the swarm has started; otherwise, <b>false</b>.
    /// </value>
    /// <seealso cref="StartAsync"/>
    /// <seealso cref="StopAsync"/>
    public bool IsRunning { get; private set; }

    /// <summary>
    ///   Cancellation tokens for the listeners.
    /// </summary>
    private readonly ConcurrentDictionary<MultiAddress, CancellationTokenSource> _listeners = new();

    /// <summary>
    ///   Get the sequence of all known peer addresses.
    /// </summary>
    /// <value>
    ///   Contains any peer address that has been
    ///   <see cref="RegisterPeerAddress">discovered</see>.
    /// </value>
    /// <seealso cref="RegisterPeerAddress"/>
    public IEnumerable<MultiAddress> KnownPeerAddresses
    {
        get
        {
            return _otherPeers
                .Values
                .SelectMany(p => p.Addresses);
        }
    }

    /// <summary>
    ///   Get the sequence of all known peers.
    /// </summary>
    /// <value>
    ///   Contains any peer that has been
    ///   <see cref="RegisterPeerAddress">discovered</see>.
    /// </value>
    /// <seealso cref="RegisterPeerAddress"/>
    public IEnumerable<Peer> KnownPeers
    {
        get
        {
            return _otherPeers.Values;
        }
    }

    /// <summary>
    ///   Register that a peer's address has been discovered.
    /// </summary>
    /// <param name="address">
    ///   An address to the peer. It must end with the peer ID.
    /// </param>
    /// <returns>
    ///   The <see cref="Peer"/> that is registered.
    /// </returns>
    /// <exception cref="Exception">
    ///   The <see cref="BlackList"/> or <see cref="WhiteList"/> policies forbid it.
    ///   Or the "p2p/ipfs" protocol name is missing.
    /// </exception>
    /// <remarks>
    ///   If the <paramref name="address"/> is not already known, then it is
    ///   added to the <see cref="KnownPeerAddresses"/>.
    /// </remarks>
    /// <seealso cref="RegisterPeer(Peer)"/>
    public Peer RegisterPeerAddress(MultiAddress address)
    {
        var peer = new Peer
        {
            Id = address.PeerId,
            Addresses = new List<MultiAddress> { address }
        };

        return RegisterPeer(peer);
    }

    /// <summary>
    ///   Register that a peer has been discovered.
    /// </summary>
    /// <param name="peer">
    ///   The newly discovered peer.
    /// </param>
    /// <returns>
    ///   The registered peer.
    /// </returns>
    /// <remarks>
    ///   If the peer already exists, then the existing peer is updated with supplied
    ///   information and is then returned.  Otherwise, the <paramref name="peer"/>
    ///   is added to known peers and is returned.
    ///   <para>
    ///   If the peer already exists, then a union of the existing and new addresses
    ///   is used.  For all other information the <paramref name="peer"/>'s information
    ///   is used if not <b>null</b>.
    ///   </para>
    ///   <para>
    ///   If peer does not already exist, then the <see cref="PeerDiscovered"/> event
    ///   is raised.
    ///   </para>
    /// </remarks>
    /// <exception cref="Exception">
    ///   The <see cref="BlackList"/> or <see cref="WhiteList"/> policies forbid it.
    /// </exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public Peer RegisterPeer(Peer peer)
    {
        if (peer.Id == null)
        {
            throw new ArgumentNullException("peer.ID");
        }
        if (peer.Id == LocalPeer.Id)
        {
            throw new ArgumentException("Cannot register self.");
        }
        if (!IsAllowed(peer))
        {
            throw new Exception($"Communication with '{peer}' is not allowed.");
        }

        var isNew = false;
        var p = _otherPeers.AddOrUpdate(peer.Id.ToBase58(),
            (_) =>
            {
                isNew = true;
                return peer;
            },
            (_, existing) =>
            {
                if (!ReferenceEquals(existing, peer))
                {
                    existing.AgentVersion = peer.AgentVersion ?? existing.AgentVersion;
                    existing.ProtocolVersion = peer.ProtocolVersion ?? existing.ProtocolVersion;
                    existing.PublicKey = peer.PublicKey ?? existing.PublicKey;
                    existing.Latency = peer.Latency ?? existing.Latency;
                    existing.Addresses = existing
                        .Addresses
                        .Union(peer.Addresses)
                        .ToList();
                }
                return existing;
            });

        if (isNew)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug($"New peer registerd {p}");
            }
            PeerDiscovered?.Invoke(this, p);
        }

        return p;
    }

    /// <summary>
    ///   Deregister a peer.
    /// </summary>
    /// <param name="peer">
    ///   The peer to remove..
    /// </param>
    /// <remarks>
    ///   Remove all knowledge of the peer. The <see cref="PeerRemoved"/> event
    ///   is raised.
    /// </remarks>
    /// <exception cref="ArgumentNullException"></exception>
    public void DeregisterPeer(Peer peer)
    {
        if (peer.Id == null)
        {
            throw new ArgumentNullException("peer.ID");
        }

        if (_otherPeers.TryRemove(peer.Id.ToBase58(), out Peer found))
        {
            peer = found;
        }
        PeerRemoved?.Invoke(this, peer);
    }

    /// <summary>
    ///   Determines if a connection is being made to the peer.
    /// </summary>
    /// <param name="peer">
    ///   A <see cref="Peer"/>.
    /// </param>
    /// <returns>
    ///   <b>true</b> is the <paramref name="peer"/> has a pending connection.
    /// </returns>
    public bool HasPendingConnection(Peer peer)
    {
        return _pendingConnections.TryGetValue(peer, out AsyncLazy<PeerConnection> _);
    }

    /// <summary>
    ///   The addresses that cannot be used.
    /// </summary>
    public MultiAddressBlackList BlackList { get; set; } = new MultiAddressBlackList();

    /// <summary>
    ///   The addresses that can be used.
    /// </summary>
    public MultiAddressWhiteList WhiteList { get; set; } = new MultiAddressWhiteList();

    /// <inheritdoc />
    public Task StartAsync()
    {
        if (LocalPeer == null)
        {
            throw new NotSupportedException("The LocalPeer is not defined.");
        }

        // Many of the unit tests do not setup the LocalPeerKey.  If
        // its missing, then just use plaintext connection.
        // TODO: make the tests setup the security protocols.
        if (LocalPeerKey == null)
        {
            lock (_protocols)
            {
                var security = _protocols.OfType<IEncryptionProtocol>().ToArray();
                foreach (var p in security)
                {
                    _protocols.Remove(p);
                }
                _protocols.Add(_plaintext1);
            }
            log.Warn("Peer key is missing, using unencrypted connections.");
        }

        Manager.PeerDisconnected += OnPeerDisconnected;
        IsRunning = true;
        _swarmCancellation = new CancellationTokenSource();
        log.Debug("Started");

        return Task.CompletedTask;
    }

    private void OnPeerDisconnected(object sender, MultiHash peerId)
    {
        if (!_otherPeers.TryGetValue(peerId.ToBase58(), out Peer peer))
        {
            peer = new Peer { Id = peerId };
        }
        PeerDisconnected?.Invoke(this, peer);
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        IsRunning = false;
        _swarmCancellation?.Cancel(true);

        log.Debug($"Stopping {LocalPeer}");

        // Stop the listeners.
        while (!_listeners.IsEmpty)
        {
            await StopListeningAsync(_listeners.Keys.First()).ConfigureAwait(false);
        }

        // Disconnect from remote peers.
        Manager.Clear();
        Manager.PeerDisconnected -= OnPeerDisconnected;

        _otherPeers.Clear();
        _listeners.Clear();
        _pendingConnections.Clear();
        _pendingRemoteConnections.Clear();
        BlackList = new MultiAddressBlackList();
        WhiteList = new MultiAddressWhiteList();

        log.Debug($"Stopped {LocalPeer}");
    }

    /// <summary>
    ///   Connect to a peer using the specified <see cref="MultiAddress"/>.
    /// </summary>
    /// <param name="address">
    ///   An ipfs <see cref="MultiAddress"/>, such as
    ///  <c>/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ</c>.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task's result
    ///   is the <see cref="PeerConnection"/>.
    /// </returns>
    /// <remarks>
    ///   If already connected to the peer and is active on any address, then
    ///   the existing connection is returned.
    /// </remarks>
    public async Task<PeerConnection> ConnectAsync(MultiAddress address, CancellationToken cancel = default)
    {
        var peer = RegisterPeerAddress(address);
        return await ConnectAsync(peer, cancel).ConfigureAwait(false);
    }

    /// <summary>
    ///   Connect to a peer.
    /// </summary>
    /// <param name="peer">
    ///  A peer to connect to.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task's result
    ///   is the <see cref="PeerConnection"/>.
    /// </returns>
    /// <remarks>
    ///   If already connected to the peer and is active on any address, then
    ///   the existing connection is returned.
    /// </remarks>
    public async Task<PeerConnection> ConnectAsync(Peer peer, CancellationToken cancel = default)
    {
        if (!IsRunning)
        {
            throw new Exception("The swarm is not running.");
        }

        peer = RegisterPeer(peer);

        // If connected and still open, then use the existing connection.
        if (Manager.TryGet(peer, out PeerConnection conn))
        {
            return conn;
        }

        // Use a current connection attempt to the peer or create a new one.
        try
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_swarmCancellation.Token, cancel))
            {
                return await _pendingConnections
                    .GetOrAdd(peer, (_) => new AsyncLazy<PeerConnection>(() => DialAsync(peer, peer.Addresses, cts.Token)))
                    .ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            PeerNotReachable?.Invoke(this, peer);
            throw;
        }
        finally
        {
            _pendingConnections.TryRemove(peer, out AsyncLazy<PeerConnection> _);
        }
    }

    /// <summary>
    ///   Create a stream to the peer that talks the specified protocol.
    /// </summary>
    /// <param name="peer">
    ///   The remote peer.
    /// </param>
    /// <param name="protocol">
    ///   The protocol name, such as "/foo/0.42.0".
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task's result
    ///   is the new <see cref="Stream"/> to the <paramref name="peer"/>.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///   When finished, the caller must <see cref="Stream.Dispose()"/> the
    ///   new stream.
    ///   </para>
    /// </remarks>
    public async Task<Stream> DialAsync(Peer peer, string protocol, CancellationToken cancel = default)
    {
        peer = RegisterPeer(peer);

        // Get a connection and then a muxer to the peer.
        var connection = await ConnectAsync(peer, cancel).ConfigureAwait(false);
        var muxer = await connection.MuxerEstablished.Task.ConfigureAwait(false);

        // Create a new stream for the peer protocol.
        var stream = await muxer.CreateStreamAsync(protocol).ConfigureAwait(false);
        try
        {
            await connection.EstablishProtocolAsync("/multistream/", stream).ConfigureAwait(false);

            await Message.WriteAsync(protocol, stream, cancel).ConfigureAwait(false);
            var result = await Message.ReadStringAsync(stream, cancel).ConfigureAwait(false);
            if (result != protocol)
            {
                throw new Exception($"Protocol '{protocol}' not supported by '{peer}'.");
            }

            return stream;
        }
        catch (Exception)
        {
            _ = stream.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    ///   Establish a duplex stream between the local and remote peer.
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="addrs"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    private async Task<PeerConnection> DialAsync(Peer remote, IEnumerable<MultiAddress> addrs, CancellationToken cancel)
    {
        log.Debug($"Dialing {remote}");

        if (remote == LocalPeer)
        {
            throw new Exception("Cannot dial self.");
        }

        // If no addresses, then ask peer routing.
        if (Router != null && !addrs.Any())
        {
            var found = await Router.FindPeerAsync(remote.Id, cancel).ConfigureAwait(false);
            addrs = found.Addresses;
            remote.Addresses = addrs;
        }

        // Get the addresses we can use to dial the remote.  Filter
        // out any addresses (ip and port) we are listening on.
        var blackList = _listeners.Keys
            .Select(a => a.WithoutPeerId())
            .ToArray();
        var possibleAddresses = (await Task.WhenAll(addrs.Select(a => a.ResolveAsync(cancel))).ConfigureAwait(false))
            .SelectMany(a => a)
            .Where(a => !blackList.Contains(a.WithoutPeerId()))
            .Select(a => a.WithPeerId(remote.Id))
            .Distinct()
            .ToArray();
        if (possibleAddresses.Length == 0)
        {
            throw new Exception($"{remote} has no known or reachable address.");
        }

        // Try the various addresses in parallel.  The first one to complete wins.
        PeerConnection connection = null;
        try
        {
            using (var timeout = new CancellationTokenSource(TransportConnectionTimeout))
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancel))
            {
                var attempts = possibleAddresses
                    .Select(a => DialAsync(remote, a, cts.Token));
                connection = await TaskHelper.WhenAnyResultAsync(attempts, cts.Token).ConfigureAwait(false);
                cts.Cancel(); // stop other dialing tasks.
            }
        }
        catch (Exception e)
        {
            var attemped = string.Join(", ", possibleAddresses.Select(a => a.ToString()));
            log.Trace($"Cannot dial {attemped}");
            throw new Exception($"Cannot dial {remote}.", e);
        }

        // Do the connection handshake.
        try
        {
            MountProtocols(connection);
            IEncryptionProtocol[] security = null;
            lock (_protocols)
            {
                security = _protocols.OfType<IEncryptionProtocol>().ToArray();
            }
            await connection.InitiateAsync(security, cancel).ConfigureAwait(false);
            await connection.MuxerEstablished.Task.ConfigureAwait(false);
            Identify1 identify = null;
            lock (_protocols)
            {
                identify = _protocols.OfType<Identify1>().First();
            }
            await identify.GetRemotePeerAsync(connection, cancel).ConfigureAwait(false);
        }
        catch (Exception)
        {
            connection.Dispose();
            throw;
        }

        var actual = Manager.Add(connection);
        if (actual == connection)
        {
            ConnectionEstablished?.Invoke(this, connection);
        }

        return actual;
    }

    private async Task<PeerConnection> DialAsync(Peer remote, MultiAddress addr, CancellationToken cancel)
    {
        // TODO: HACK: Currenty only the ipfs/p2p is supported.
        // short circuit to make life faster.
        if (addr.Protocols.Count != 3 || !(addr.Protocols[2].Name == "ipfs" || addr.Protocols[2].Name == "p2p"))
        {
            throw new Exception($"Cannnot dial; unknown protocol in '{addr}'.");
        }

        // Establish the transport stream.
        Stream stream = null;
        foreach (var protocol in addr.Protocols)
        {
            cancel.ThrowIfCancellationRequested();
            if (TransportRegistry.Transports.TryGetValue(protocol.Name, out Func<IPeerTransport> transport))
            {
                stream = await transport().ConnectAsync(addr, cancel).ConfigureAwait(false);
                if (cancel.IsCancellationRequested)
                {
                    stream?.DisposeAsync();
                    continue;
                }
                break;
            }
        }
        if (stream == null)
        {
            throw new Exception("Missing a known transport protocol name.");
        }

        // Build the connection.
        var connection = new PeerConnection
        {
            IsIncoming = false,
            LocalPeer = LocalPeer,
            LocalAddress = LocalPeer?.ConnectedAddress,
            LocalPeerKey = LocalPeerKey,
            RemotePeer = remote,
            RemoteAddress = addr,
            Stream = stream
        };

        // Are we communicating to a private network?
        if (NetworkProtector != null)
        {
            connection.Stream = await NetworkProtector.ProtectAsync(connection).ConfigureAwait(false);
        }

        return connection;
    }

    /// <summary>
    ///   Disconnect from a peer.
    /// </summary>
    /// <param name="address">
    ///   An ipfs <see cref="MultiAddress"/>, such as
    ///  <c>/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ</c>.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///   If the peer is not conected, then nothing happens.
    /// </remarks>
    public Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default)
    {
        Manager.Remove(address.PeerId);
        return Task.CompletedTask;
    }

    /// <summary>
    ///   Start listening on the specified <see cref="MultiAddress"/>.
    /// </summary>
    /// <param name="address">
    ///   Typically "/ip4/0.0.0.0/tcp/4001" or "/ip6/::/tcp/4001".
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation.  The task's result
    ///   is a <see cref="MultiAddress"/> than can be used by another peer
    ///   to connect to tis peer.
    /// </returns>
    /// <exception cref="Exception">
    ///   Already listening on <paramref name="address"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <paramref name="address"/> is missing a transport protocol (such as tcp or udp).
    /// </exception>
    /// <remarks>
    ///   Allows other peers to <see cref="ConnectAsync(MultiAddress, CancellationToken)">connect</see>
    ///   to the <paramref name="address"/>.
    ///   <para>
    ///   The <see cref="Peer.Addresses"/> of the <see cref="LocalPeer"/> are updated.  If the <paramref name="address"/> refers to
    ///   any IP address ("/ip4/0.0.0.0" or "/ip6/::") then all network interfaces addresses
    ///   are added.  If the port is zero (as in "/ip6/::/tcp/0"), then the peer addresses contains the actual port number
    ///   that was assigned.
    ///   </para>
    /// </remarks>
    public Task<MultiAddress> StartListeningAsync(MultiAddress address)
    {
        var cancel = new CancellationTokenSource();

        if (!_listeners.TryAdd(address, cancel))
        {
            throw new Exception($"Already listening on '{address}'.");
        }

        // Start a listener for the transport
        var didSomething = false;
        foreach (var protocol in address.Protocols)
        {
            if (TransportRegistry.Transports.TryGetValue(protocol.Name, out Func<IPeerTransport> transport))
            {
                address = transport().Listen(address, OnRemoteConnect, cancel.Token);
                _listeners.TryAdd(address, cancel);
                didSomething = true;
                break;
            }
        }
        if (!didSomething)
        {
            throw new ArgumentException($"Missing a transport protocol name '{address}'.", nameof(address));
        }

        var result = new MultiAddress($"{address}/ipfs/{LocalPeer.Id}");

        // Get the actual IP address(es).
        IEnumerable<MultiAddress> addresses = new List<MultiAddress>();
        var ips = NetworkInterface.GetAllNetworkInterfaces()
            // It appears that the loopback adapter is not UP on Ubuntu 14.04.5 LTS
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                          || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses);
        if (result.ToString().StartsWith("/ip4/0.0.0.0/"))
        {
            addresses = ips
                .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip =>
                {
                    return new MultiAddress(result.ToString().Replace("0.0.0.0", ip.Address.ToString()));
                })
                .ToArray();
        }
        else if (result.ToString().StartsWith("/ip6/::/"))
        {
            addresses = ips
                .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(ip =>
                {
                    return new MultiAddress(result.ToString().Replace("::", ip.Address.ToString()));
                })
                .ToArray();
        }
        else
        {
            addresses = new MultiAddress[] { result };
        }
        if (!addresses.Any())
        {
            var msg = "Cannot determine address(es) for " + result;
            foreach (var ip in ips)
            {
                msg += " nic-ip: " + ip.Address.ToString();
            }
            cancel.Cancel();
            throw new Exception(msg);
        }

        // Add actual addresses to listeners and local peer addresses.
        foreach (var a in addresses)
        {
            log.Debug($"Listening on {a}");
            _listeners.TryAdd(a, cancel);
        }
        LocalPeer.Addresses = LocalPeer
            .Addresses
            .Union(addresses)
            .ToArray();

        ListenerEstablished?.Invoke(this, LocalPeer);
        return Task.FromResult(addresses.First());
    }

#pragma warning disable VSTHRD100 // Avoid async void methods

    /// <summary>
    ///   Called when a remote peer is connecting to the local peer.
    /// </summary>
    /// <param name="stream">
    ///   The stream to the remote peer.
    /// </param>
    /// <param name="local">
    ///   The local peer's address.
    /// </param>
    /// <param name="remote">
    ///   The remote peer's address.
    /// </param>
    /// <remarks>
    ///   Establishes the protocols of the connection.  Any exception is simply
    ///   logged as warning.
    /// </remarks>
    private async void OnRemoteConnect(Stream stream, MultiAddress local, MultiAddress remote)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        if (!IsRunning)
        {
            try
            {
                stream.Dispose();
            }
            catch
            {
                // eat it.
            }
            return;
        }

        // If the remote is already trying to establish a connection, then we
        // can just refuse this one.
        if (!_pendingRemoteConnections.TryAdd(remote, null))
        {
            log.Debug($"Duplicate remote connection from {remote}");
            try
            {
                stream.Dispose();
            }
            catch
            {
                // eat it.
            }
            return;
        }

        try
        {
            if (log.IsDebugEnabled)
            {
                log.Debug($"remote connect from {remote}");
            }

            // TODO: Check the policies

            var connection = new PeerConnection
            {
                IsIncoming = true,
                LocalPeer = LocalPeer,
                LocalAddress = local,
                LocalPeerKey = LocalPeerKey,
                RemoteAddress = remote,
                Stream = stream
            };

            // Are we communicating to a private network?
            if (NetworkProtector != null)
            {
                connection.Stream = await NetworkProtector.ProtectAsync(connection).ConfigureAwait(false);
            }

            // Mount the protocols.
            MountProtocols(connection);

            // Start the handshake
            // TODO: Isn't connection cancel token required.
            _ = connection.ReadMessagesAsync(default);

            // Wait for security to be established.
            await connection.SecurityEstablished.Task.ConfigureAwait(false);
            // TODO: Maybe connection.LocalPeerKey = null;

            // Wait for the handshake to complete.
            var muxer = await connection.MuxerEstablished.Task;

            // Need details on the remote peer.
            Identify1 identify = null;
            lock (_protocols)
            {
                identify = _protocols.OfType<Identify1>().First();
            }
            connection.RemotePeer = await identify.GetRemotePeerAsync(connection, default).ConfigureAwait(false);

            connection.RemotePeer = RegisterPeer(connection.RemotePeer);
            connection.RemoteAddress = new MultiAddress($"{remote}/ipfs/{connection.RemotePeer.Id}");
            var actual = Manager.Add(connection);
            if (actual == connection)
            {
                ConnectionEstablished?.Invoke(this, connection);
            }
        }
        catch (Exception e)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug($"remote connect from {remote} failed: {e.Message}");
            }
            try
            {
                stream.Dispose();
            }
            catch
            {
                // eat it.
            }
        }
        finally
        {
            _pendingRemoteConnections.TryRemove(remote, out object _);
        }
    }

    /// <summary>
    ///   Add a protocol that is supported by the swarm.
    /// </summary>
    /// <param name="protocol">
    ///   The protocol to add.
    /// </param>
    public void AddProtocol(IPeerProtocol protocol)
    {
        lock (_protocols)
        {
            _protocols.Add(protocol);
        }
    }

    /// <summary>
    ///   Remove a protocol from the swarm.
    /// </summary>
    /// <param name="protocol">
    ///   The protocol to remove.
    /// </param>
    public void RemoveProtocol(IPeerProtocol protocol)
    {
        lock (_protocols)
        {
            _protocols.Remove(protocol);
        }
    }

    private void MountProtocols(PeerConnection connection)
    {
        lock (_protocols)
        {
            connection.AddProtocols(_protocols);
        }
    }

    /// <summary>
    ///   Stop listening on the specified <see cref="MultiAddress"/>.
    /// </summary>
    /// <param name="address"></param>
    /// <returns>
    ///   A task that represents the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///   Allows other peers to <see cref="ConnectAsync(MultiAddress, CancellationToken)">connect</see>
    ///   to the <paramref name="address"/>.
    ///   <para>
    ///   The addresses of the <see cref="LocalPeer"/> are updated.
    ///   </para>
    /// </remarks>
    public async Task StopListeningAsync(MultiAddress address)
    {
        if (!_listeners.TryRemove(address, out CancellationTokenSource listener))
        {
            return;
        }

        try
        {
            if (!listener.IsCancellationRequested)
            {
                listener.Cancel(false);
            }

            // Remove any local peer address that depend on the cancellation token.
            var others = _listeners
                .Where(l => l.Value == listener)
                .Select(l => l.Key)
                .ToArray();

            LocalPeer.Addresses = LocalPeer.Addresses
                .Where(a => a != address && !others.Contains(a))
                .ToArray();

            foreach (var other in others)
            {
                _listeners.TryRemove(other, out CancellationTokenSource _);
            }

            // Give some time away, so that cancel can run
            // TODO: Would be nice to make this deterministic.
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            log.Error("stop listening failed", e);
        }
    }

    /// <inheritdoc />
    public bool IsAllowed(MultiAddress target)
    {
        return BlackList.IsAllowed(target)
               && WhiteList.IsAllowed(target);
    }

    /// <inheritdoc />
    public bool IsAllowed(Peer peer)
    {
        return peer.Addresses.All(IsAllowed);
    }
}