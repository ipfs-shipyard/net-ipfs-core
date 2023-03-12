using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk.Cryptography;
using IpfsShipyard.PeerTalk.Multiplex;
using IpfsShipyard.PeerTalk.Protocols;

namespace IpfsShipyard.PeerTalk;

/// <summary>
///   A connection between two peers.
/// </summary>
/// <remarks>
///   A connection is used to exchange messages between peers.
/// </remarks>
public class PeerConnection : IDisposable
{
    private static readonly ILog log = LogManager.GetLogger(typeof(PeerConnection));

    private Stream _stream;
    private StatsStream _statsStream;

    /// <summary>
    ///   The local peer.
    /// </summary>
    public Peer LocalPeer { get; set; }

    /// <summary>
    ///   The remote peer.
    /// </summary>
    public Peer RemotePeer { get; set; }

    /// <summary>
    ///   The local peer's end point.
    /// </summary>
    public MultiAddress LocalAddress { get; set; }

    /// <summary>
    ///   The remote peer's end point.
    /// </summary>
    public MultiAddress RemoteAddress { get; set; }

    /// <summary>
    ///   The private key of the local peer.
    /// </summary>
    /// <value>
    ///   Used to prove the identity of the <see cref="LocalPeer"/>.
    /// </value>
    public Key LocalPeerKey { get; set; }

    /// <summary>
    ///   Determine which peer (local or remote) initiated the connection.
    /// </summary>
    /// <value>
    ///   <b>true</b> if the <see cref="RemotePeer"/> initiated the connection;
    ///   otherwise, <b>false</b>.
    /// </value>
    public bool IsIncoming { get; set; }

    /// <summary>
    ///   Determines if the connection to the remote can be used.
    /// </summary>
    /// <value>
    ///   <b>true</b> if the connection is active.
    /// </value>
    public bool IsActive
    {
        get { return Stream?.CanRead == true && Stream.CanWrite; }
    }

    /// <summary>
    ///   The duplex stream between the two peers.
    /// </summary>
    public Stream Stream
    {
        get { return _stream; }
        set
        {
            if (value != null && _statsStream == null)
            {
                _statsStream = new StatsStream(value);
                value = _statsStream;
            }
            _stream = value;
        }
    }

    /// <summary>
    ///   The protocols that the connection will handle.
    /// </summary>
    /// <value>
    ///   The key is a protocol name, such as "/mplex/6.7.0".  The value
    ///   is a function that will process the protocol message.
    /// </value>
    /// <seealso cref="AddProtocol"/>
    /// <seealso cref="AddProtocols"/>
    public Dictionary<string, Func<PeerConnection, Stream, CancellationToken, Task>> Protocols { get; }
        = new Dictionary<string, Func<PeerConnection, Stream, CancellationToken, Task>>();

    /// <summary>
    ///   Add a protocol that the connection will handle.
    /// </summary>
    /// <param name="protocol">
    ///   A peer protocol to add.
    /// </param>
    public void AddProtocol(IPeerProtocol protocol)
    {
        Protocols.Add(protocol.ToString(), protocol.ProcessMessageAsync);
    }

    /// <summary>
    ///   Add a seequence of protocols that the connection will handle.
    /// </summary>
    /// <param name="protocols">
    ///   The peer protocols to add.
    /// </param>
    public void AddProtocols(IEnumerable<IPeerProtocol> protocols)
    {
        foreach (var protocol in protocols)
        {
            if (protocol != null)
            {
                Protocols.Add(protocol.ToString(), protocol.ProcessMessageAsync);
            }
        }
    }

    /// <summary>
    ///   Signals that the security for the connection is established.
    /// </summary>
    /// <remarks>
    ///   This can be awaited.
    /// </remarks>
    public TaskCompletionSource<bool> SecurityEstablished { get; } = new TaskCompletionSource<bool>();

    /// <summary>
    ///   Signals that the muxer for the connection is established.
    /// </summary>
    /// <remarks>
    ///   This can be awaited.
    /// </remarks>
    public TaskCompletionSource<Muxer> MuxerEstablished { get; } = new TaskCompletionSource<Muxer>();

    /// <summary>
    ///   Signals that the identity of the remote endpoint is established.
    /// </summary>
    /// <remarks>
    ///   This can be awaited.
    /// </remarks>
    /// <remarks>
    ///   The data in <see cref="RemotePeer"/> is not complete until
    ///   the identity is establish.
    /// </remarks>
    public TaskCompletionSource<Peer> IdentityEstablished { get; } = new TaskCompletionSource<Peer>();

    /// <summary>
    ///   When the connection was last used.
    /// </summary>
    public DateTime LastUsed => _statsStream.LastUsed;

    /// <summary>
    ///   Number of bytes read over the connection.
    /// </summary>
    public long BytesRead => _statsStream.BytesRead;

    /// <summary>
    ///   Number of bytes written over the connection.
    /// </summary>
    public long BytesWritten => _statsStream.BytesWritten;

    /// <summary>
    ///  Establish the connection with the remote node.
    /// </summary>
    /// <param name="securityProtocols"></param>
    /// <param name="cancel"></param>
    /// <remarks>
    ///   This should be called when the local peer wants a connection with
    ///   the remote peer.
    /// </remarks>
    public async Task InitiateAsync(
        IEnumerable<IEncryptionProtocol> securityProtocols,
        CancellationToken cancel = default)
    {
        await EstablishProtocolAsync("/multistream/", cancel).ConfigureAwait(false);

        // Find the first security protocol that is also supported by the remote.
        var exceptions = new List<Exception>();
        foreach (var protocol in securityProtocols)
        {
            try
            {
                await EstablishProtocolAsync(protocol.ToString(), cancel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                continue;
            }

            await protocol.EncryptAsync(this, cancel).ConfigureAwait(false);
            break;
        }
        if (!SecurityEstablished.Task.IsCompleted)
            throw new AggregateException("Could not establish a secure connection.", exceptions);

        await EstablishProtocolAsync("/multistream/", cancel).ConfigureAwait(false);
        await EstablishProtocolAsync("/mplex/", cancel).ConfigureAwait(false);

        var muxer = new Muxer
        {
            Channel = Stream,
            Initiator = true,
            Connection = this
        };
        muxer.SubstreamCreated += (_, e) => _ = ReadMessagesAsync(e, CancellationToken.None);
        MuxerEstablished.SetResult(muxer);

        _ = muxer.ProcessRequestsAsync(cancel);
    }

    /// <summary>
    ///   TODO:
    /// </summary>
    /// <param name="name"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    public Task EstablishProtocolAsync(string name, CancellationToken cancel)
    {
        return EstablishProtocolAsync(name, Stream, cancel);
    }

    /// <summary>
    ///   TODO:
    /// </summary>
    /// <param name="name"></param>
    /// <param name="stream"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    public async Task EstablishProtocolAsync(string name, Stream stream, CancellationToken cancel = default)
    {
        var protocols = ProtocolRegistry.Protocols.Keys
            .Where(k => k == name || k.StartsWith(name))
            .Select(k => VersionedName.Parse(k))
            .OrderByDescending(vn => vn)
            .Select(vn => vn.ToString());
        foreach (var protocol in protocols)
        {
            await Message.WriteAsync(protocol, stream, cancel).ConfigureAwait(false);
            var result = await Message.ReadStringAsync(stream, cancel).ConfigureAwait(false);
            if (result == protocol)
            {
                return;
            }
        }
        if (!protocols.Any())
        {
            throw new Exception($"Protocol '{name}' is not registered.");
        }
        throw new Exception($"{RemotePeer.Id} does not support protocol '{name}'.");
    }

    /// <summary>
    ///   Starts reading messages from the remote peer.
    /// </summary>
    public async Task ReadMessagesAsync(CancellationToken cancel)
    {
        log.Debug($"start reading messsages from {RemoteAddress}");

        // TODO: Only a subset of protocols are allowed until
        // the remote is authenticated.
        IPeerProtocol protocol = new Multistream1();
        try
        {
            while (!cancel.IsCancellationRequested && Stream != null)
            {
                await protocol.ProcessMessageAsync(this, Stream, cancel).ConfigureAwait(false);
            }
        }
        catch (IOException e)
        {
            log.Error("reading message failed " + e.Message);
            // eat it.
        }
        catch (Exception e)
        {
            if (!cancel.IsCancellationRequested && Stream != null)
            {
                log.Error("reading message failed", e);
            }
        }

        // Ignore any disposal exceptions.
        try
        {
            Stream?.DisposeAsync();
        }
        catch
        {
            // eat it.
        }

        log.Debug($"stop reading messsages from {RemoteAddress}");
    }

    /// <summary>
    ///   Starts reading messages from the remote peer on the specified stream.
    /// </summary>
    public async Task ReadMessagesAsync(Stream stream, CancellationToken cancel)
    {
        IPeerProtocol protocol = new Multistream1();
        try
        {
            while (!cancel.IsCancellationRequested && stream?.CanRead == true)
            {
                await protocol.ProcessMessageAsync(this, stream, cancel).ConfigureAwait(false);
            }
        }
        catch (EndOfStreamException)
        {
            // eat it.
        }
        catch (Exception e)
        {
            if (!cancel.IsCancellationRequested && stream != null)
            {
                log.Error($"reading message failed {RemoteAddress} {RemotePeer}", e);
            }
        }
    }

    #region IDisposable Support

    private bool _disposedValue = false; // To detect redundant calls

    /// <summary>
    ///   Signals that the connection is closed (disposed).
    /// </summary>
    public event EventHandler<PeerConnection> Closed;

    /// <summary>
    ///  TODO
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;
        _disposedValue = true;

        if (disposing)
        {
            log.Debug($"Closing connection to {RemoteAddress}");
            if (Stream != null)
            {
                try
                {
                    Stream.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // ignore stream already closed.
                }
                catch (Exception e)
                {
                    log.Warn($"Failed to close connection to {RemoteAddress}", e);
                    // eat it.
                }
                finally
                {
                    Stream = null;
                    _statsStream = null;
                }
            }
            SecurityEstablished.TrySetCanceled();
            IdentityEstablished.TrySetCanceled();
            IdentityEstablished.TrySetCanceled();
            Closed?.Invoke(this, this);
        }

        // free unmanaged resources (unmanaged objects) and override a finalizer below.
        // set large fields to null.
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~PeerConnection() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support
}