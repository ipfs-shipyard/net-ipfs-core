using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using Nito.AsyncEx;

namespace IpfsShipyard.PeerTalk.Multiplex
{
    /// <summary>
    ///   Supports multiple protocols over a single channel (stream).
    /// </summary>
    /// <remarks>
    ///   See <see href="https://github.com/libp2p/mplex"/> for the spec.
    /// </remarks>
    public class Muxer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Muxer));

        /// <summary>
        ///   The next stream ID to create.
        /// </summary>
        /// <value>
        ///   The session initiator allocates even IDs and the session receiver allocates odd IDs.
        /// </value>
        public long NextStreamId { get; private set; } = 1000;

        /// <summary>
        ///   The signle channel to exchange protocol messages.
        /// </summary>
        /// <value>
        ///   A <see cref="Stream"/> to exchange protocol messages.
        /// </value>
        public Stream Channel { get; set; }

        /// <summary>
        ///   The peer connection.
        /// </summary>
        /// <value>
        ///   The peer connection that owns this muxer.
        /// </value>
        public PeerConnection Connection { get; set; }

        /// <summary>
        ///   Raised when the remote end creates a new stream.
        /// </summary>
        public event EventHandler<Substream> SubstreamCreated;

        /// <summary>
        ///   Raised when the remote end closes a stream.
        /// </summary>
        public event EventHandler<Substream> SubstreamClosed;

        private readonly AsyncLock ChannelWriteLock = new();

        /// <summary>
        ///   The substreams that are open.
        /// </summary>
        /// <value>
        ///   The key is stream ID and the value is a <see cref="Substream"/>.
        /// </value>
        public ConcurrentDictionary<long, Substream> Substreams = new();

        /// <summary>
        ///   Determines if the muxer is the initiator.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the muxer is the initiator.
        /// </value>
        /// <seealso cref="Receiver"/>
        public bool Initiator
        {
            get { return (NextStreamId & 1) == 0; }
            set
            {
                if (value != Initiator)
                    NextStreamId++;
            }
        }

        /// <summary>
        ///   Determines if the muxer is the receiver.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the muxer is the receiver.
        /// </value>
        /// <seealso cref="Initiator"/>
        public bool Receiver
        {
            get { return !Initiator; }
            set { Initiator = !value; }
        }

        /// <summary>
        ///   Creates a new stream with the specified name.
        /// </summary>
        /// <param name="name">
        ///   A name for the stream.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A duplex stream.
        /// </returns>
        public async Task<Substream> CreateStreamAsync(string name = "", CancellationToken cancel = default)
        {
            var streamId = NextStreamId;
            NextStreamId += 2;
            var substream = new Substream
            {
                Id = streamId,
                Name = name,
                Muxer = this,
                SentMessageType = PacketType.MessageInitiator,
            };
            Substreams.TryAdd(streamId, substream);

            // Tell the other side about the new stream.
            using (await AcquireWriteAccessAsync().ConfigureAwait(false))
            {
                var header = new Header { StreamId = streamId, PacketType = PacketType.NewStream };
                var wireName = Encoding.UTF8.GetBytes(name);
                await header.WriteAsync(Channel).ConfigureAwait(false);
                await Channel.WriteVarintAsync(wireName.Length, cancel).ConfigureAwait(false);
                await Channel.WriteAsync(wireName, 0, wireName.Length).ConfigureAwait(false);
                await Channel.FlushAsync(cancel).ConfigureAwait(false);
            }
            return substream;
        }

        /// <summary>
        ///   Remove the stream.
        /// </summary>
        /// <remarks>
        ///   Internal method called by Substream.Dispose().
        /// </remarks>
        public async Task<Substream> RemoveStreamAsync(Substream stream, CancellationToken cancel = default)
        {
            if (Substreams.TryRemove(stream.Id, out Substream _))
            {
                // Tell the other side.
                using (await AcquireWriteAccessAsync().ConfigureAwait(false))
                {
                    var header = new Header
                    {
                        StreamId = stream.Id,
                        PacketType = PacketType.CloseInitiator
                    };
                    await header.WriteAsync(Channel, cancel).ConfigureAwait(false);
                    Channel.WriteByte(0); // length
                    await Channel.FlushAsync(cancel).ConfigureAwait(false);
                }
            }

            return stream;
        }

        /// <summary>
        ///   Read the multiplex packets.
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <remarks>
        ///   A background task that reads and processes the multiplex packets while
        ///   the <see cref="Channel"/> is open and not <paramref name="cancel">cancelled</paramref>.
        ///   <para>
        ///   Any encountered errors will close the <see cref="Channel"/>.
        ///   </para>
        /// </remarks>
        public async Task ProcessRequestsAsync(CancellationToken cancel = default)
        {
            try
            {
                while (Channel.CanRead && !cancel.IsCancellationRequested)
                {
                    // Read the packet prefix.
                    var header = await Header.ReadAsync(Channel, cancel).ConfigureAwait(false);
                    var length = await Channel.ReadVarint32Async(cancel).ConfigureAwait(false);
                    if (log.IsTraceEnabled)
                        log.TraceFormat("received '{0}', stream={1}, length={2}", header.PacketType, header.StreamId, length);

                    // Read the payload.
                    var payload = new byte[length];
                    await Channel.ReadExactAsync(payload, 0, length, cancel).ConfigureAwait(false);

                    // Process the packet
                    Substreams.TryGetValue(header.StreamId, out Substream substream);

                    if (header.PacketType == PacketType.NewStream)
                    {
                        if (substream != null)
                        {
                            log.Warn($"Stream {substream.Id} already exists");
                            continue;
                        }

                        substream = new Substream
                        {
                            Id = header.StreamId,
                            Name = Encoding.UTF8.GetString(payload),
                            Muxer = this
                        };

                        if (!Substreams.TryAdd(substream.Id, substream))
                        {
                            // Should not happen.
                            throw new Exception($"Stream {substream.Id} already exists");
                        }
                        SubstreamCreated?.Invoke(this, substream);

                        // Special hack for go-ipfs
                        if (Receiver && (substream.Id & 1) == 1)
                        {
                            log.Debug($"go-hack sending newstream {substream.Id}");
                            using (await AcquireWriteAccessAsync().ConfigureAwait(false))
                            {
                                var hdr = new Header
                                {
                                    StreamId = substream.Id,
                                    PacketType = PacketType.NewStream
                                };
                                await hdr.WriteAsync(Channel, cancel).ConfigureAwait(false);
                                Channel.WriteByte(0); // length
                                await Channel.FlushAsync(cancel).ConfigureAwait(false);
                            }
                        }
                    }
                    else if (header.PacketType == PacketType.MessageInitiator)
                    {
                        if (substream == null)
                        {
                            log.Warn($"Message to unknown stream #{header.StreamId}");
                            continue;
                        }
                        substream.AddData(payload);
                    }
                    else if (header.PacketType == PacketType.MessageReceiver)
                    {
                        if (substream == null)
                        {
                            log.Warn($"Message to unknown stream #{header.StreamId}");
                            continue;
                        }
                        substream.AddData(payload);
                    }
                    else if ((header.PacketType == PacketType.CloseInitiator)
                            || (header.PacketType == PacketType.CloseReceiver)
                            || (header.PacketType == PacketType.ResetInitiator)
                            || (header.PacketType == PacketType.ResetReceiver))
                    {
                        if (substream == null)
                        {
                            log.Warn($"Reset of unknown stream #{header.StreamId}");
                            continue;
                        }
                        substream.NoMoreData();
                        Substreams.TryRemove(substream.Id, out Substream _);
                        SubstreamClosed?.Invoke(this, substream);
                    }
                    else
                    {
                        throw new InvalidDataException($"Unknown Muxer packet type '{header.PacketType}'.");
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // eat it
            }
            catch (IOException)
            {
                // eat it
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                // eat it
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it
            }
            catch (Exception e)
            {
                // eat it

                // Log error if the channel is not closed.
                if (Channel.CanRead || Channel.CanWrite)
                {
                    // eat it
                    log.Error("failed", e);
                }
            }

            // Some of the tests do not pass a connection.
            if (Connection != null)
                Connection.Dispose();
            else if (Channel != null)
                await Channel.DisposeAsync();

            // Dispose of all the substreams.
            var streams = Substreams.Values.ToArray();

            Substreams.Clear();
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }

        /// <summary>
        ///   Acquire permission to write to the Channel.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   is an <see cref="IDisposable"/> that releases the lock.
        /// </returns>
        public Task<IDisposable> AcquireWriteAccessAsync()
        {
            return ChannelWriteLock.LockAsync();
        }
    }
}