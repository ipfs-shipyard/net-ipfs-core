using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
#if !NET5_0_OR_GREATER

using JuiceStream;

#endif

namespace IpfsShipyard.PeerTalk.Transports
{
    /// <summary>
    ///   Establishes a duplex stream between two peers
    ///   over TCP.
    /// </summary>
    /// <remarks>
    ///   <see cref="ConnectAsync"/> determines the network latency and sets the timeout
    ///   to 3 times the latency or <see cref="MinReadTimeout"/>.
    /// </remarks>
    public class Tcp : IPeerTransport
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Tcp));

        /// <summary>
        ///  The minimum read timeout.
        /// </summary>
        /// <value>
        ///   Defaults to 3 seconds.
        /// </value>
        public static TimeSpan MinReadTimeout = TimeSpan.FromSeconds(3);

        /// <inheritdoc />
        public async Task<Stream> ConnectAsync(MultiAddress address, CancellationToken cancel = default)
        {
            var port = address.Protocols
                .Where(p => p.Name == "tcp")
                .Select(p => int.Parse(p.Value))
                .First();
            var ip = address.Protocols
                .Find(p => p.Name == "ip4" || p.Name == "ip6");
            if (ip == null)
                throw new ArgumentException($"Missing IP address in '{address}'.", nameof(address));
            var socket = new Socket(
                ip.Name == "ip4" ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6,
                SocketType.Stream,
                ProtocolType.Tcp);

            var latency = MinReadTimeout; // keep compiler happy
            var start = DateTime.Now;
            try
            {
                log.Trace("connecting to " + address);

                // Handle cancellation of the connect attempt by disposing
                // of the socket.  This will force ConnectAsync to return.
                await using (var _ = cancel.Register(() => { socket?.Dispose(); socket = null; }))
                {
                    var ipaddr = IPAddress.Parse(ip.Value);
                    await socket.ConnectAsync(ipaddr, port).ConfigureAwait(false);
                }

                latency = DateTime.Now - start;
                log.Trace($"connected to {address} in {latency.TotalMilliseconds} ms");
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it, the caller has cancelled and doesn't care.
            }
            catch (Exception)
            {
                latency = DateTime.Now - start;
                log.Trace($"failed to {address} in {latency.TotalMilliseconds} ms");
                socket?.Dispose();
                throw;
            }
            if (cancel.IsCancellationRequested)
            {
                log.Trace("cancel " + address);
                socket?.Dispose();
                cancel.ThrowIfCancellationRequested();
            }

            var timeout = (int)Math.Max(MinReadTimeout.TotalMilliseconds, latency.TotalMilliseconds * 3);
            socket.LingerState = new(false, 0);
            socket.ReceiveTimeout = timeout;
            socket.SendTimeout = timeout;
            Stream stream = new NetworkStream(socket, ownsSocket: true);
            stream.ReadTimeout = timeout;
            stream.WriteTimeout = timeout;

#if !NET5_0_OR_GREATER
            // BufferedStream not available in .Net Standard 1.4
            stream = new DuplexBufferedStream(stream);
#endif

            if (cancel.IsCancellationRequested)
            {
                log.Trace("cancel " + address);
                await stream.DisposeAsync();
                cancel.ThrowIfCancellationRequested();
            }

            return stream;
        }

        /// <inheritdoc />
        public MultiAddress Listen(MultiAddress address, Action<Stream, MultiAddress, MultiAddress> handler, CancellationToken cancel)
        {
            var port = address.Protocols
                .Where(p => p.Name == "tcp")
                .Select(p => int.Parse(p.Value))
                .FirstOrDefault();
            var ip = address.Protocols
                .Find(p => p.Name == "ip4" || p.Name == "ip6");
            if (ip == null)
                throw new ArgumentException($"Missing IP address in '{address}'.", nameof(address));
            var ipAddress = IPAddress.Parse(ip.Value);
            var endPoint = new IPEndPoint(ipAddress, port);
            var socket = new Socket(
                endPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);
            try
            {
                socket.Bind(endPoint);
                socket.Listen(100);
            }
            catch (Exception e)
            {
                socket.Dispose();
                throw new("Bind/listen failed on " + address, e);
            }

            // If no port specified, then add it.
            var actualPort = ((IPEndPoint)socket.LocalEndPoint).Port;
            if (port != actualPort)
            {
                address = address.Clone();
                var protocol = address.Protocols.Find(p => p.Name == "tcp");
                if (protocol != null)
                {
                    protocol.Value = actualPort.ToString();
                }
                else
                {
                    address.Protocols.AddRange(new MultiAddress("/tcp/" + actualPort).Protocols);
                }
            }

            _ = Task.Run(() => ProcessConnection(socket, address, handler, cancel), cancel);

            return address;
        }

        private void ProcessConnection(Socket socket, MultiAddress address, Action<Stream, MultiAddress, MultiAddress> handler, CancellationToken cancel)
        {
            log.Debug("listening on " + address);

            // Handle cancellation of the listener
            cancel.Register(() =>
            {
                log.Debug("Got cancel on " + address);

                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Dispose();
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        socket.Shutdown(SocketShutdown.Receive);
                    }
                    else // must be windows
                    {
                        socket.Dispose();
                    }
                }
                catch (Exception e)
                {
                    log.Warn($"Cancelling listener: {e.Message}");
                }
                finally
                {
                    socket = null;
                }
            });

            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    var conn = socket.Accept();
                    MultiAddress remote = null;
                    if (conn.RemoteEndPoint is IPEndPoint endPoint)
                    {
                        var s = new StringBuilder();
                        s.Append(endPoint.AddressFamily == AddressFamily.InterNetwork ? "/ip4/" : "/ip6/");
                        s.Append(endPoint.Address);
                        s.Append("/tcp/");
                        s.Append(endPoint.Port);
                        remote = new(s.ToString());
                        log.Debug("connection from " + remote);
                    }

                    conn.NoDelay = true;
                    Stream peer = new NetworkStream(conn, ownsSocket: true);
#if !NET5_0_OR_GREATER
                    // BufferedStream not available in .Net Standard 1.4
                    peer = new DuplexBufferedStream(peer);
#endif
                    try
                    {
                        handler(peer, address, remote);
                    }
                    catch (Exception e)
                    {
                        log.Error("listener handler failed " + address, e);
                        peer.Dispose();
                    }
                }
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                log.Error("listener failed " + address, e);
                // eat it and give up
            }
            finally
            {
                socket?.Dispose();
            }

            log.Debug("stop listening on " + address);
        }
    }
}