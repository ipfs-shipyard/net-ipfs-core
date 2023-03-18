using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.PeerTalk.Transports;

/// <summary>
///   Establishes a duplex stream between two peers
///   over UDP.
/// </summary>
public class Udp : IPeerTransport
{
    private static readonly ILog log = LogManager.GetLogger(typeof(Udp));

    /// <inheritdoc />
    public async Task<Stream> ConnectAsync(MultiAddress address, CancellationToken cancel = default)
    {
        var port = address.Protocols
            .Where(p => p.Name == "udp")
            .Select(p => int.Parse(p.Value))
            .First();
        var ip = address.Protocols
            .Find(p => p.Name is "ip4" or "ip6");
        var socket = new Socket(
            ip.Name == "ip4" ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6,
            SocketType.Dgram,
            ProtocolType.Udp);

        // Handle cancellation of the connect attempt
        cancel.Register(() =>
        {
            socket.Dispose();
            socket = null;
        });

        try
        {
            log.Debug("connecting to " + address);
            await socket.ConnectAsync(ip.Value, port, cancel).ConfigureAwait(false);
            log.Debug("connected " + address);
        }
        catch (Exception) when (cancel.IsCancellationRequested)
        {
            // eat it, the caller has cancelled and doesn't care.
        }
        catch (Exception e)
        {
            log.Warn("failed " + address, e);
            throw;
        }
        if (cancel.IsCancellationRequested)
        {
            log.Debug("cancel " + address);
            socket?.Dispose();
            cancel.ThrowIfCancellationRequested();
        }
        return new DatagramStream(socket, ownsSocket: true);
    }

    /// <inheritdoc />
    public MultiAddress Listen(MultiAddress address, Action<Stream, MultiAddress, MultiAddress> handler, CancellationToken cancel)
    {
        var port = address.Protocols
            .Where(p => p.Name == "udp")
            .Select(p => int.Parse(p.Value))
            .FirstOrDefault();
        var ip = address.Protocols
            .First(p => p.Name is "ip4" or "ip6");
        var ipAddress = IPAddress.Parse(ip.Value);
        var endPoint = new IPEndPoint(ipAddress, port);
        var socket = new Socket(
            endPoint.AddressFamily,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.Bind(endPoint);

        // If no port specified, then add it.
        var actualPort = ((IPEndPoint)socket.LocalEndPoint).Port;
        if (port != actualPort)
        {
            address = address.Clone();
            var protocol = address.Protocols.Find(p => p.Name == "udp");
            if (protocol != null)
            {
                protocol.Value = actualPort.ToString();
            }
            else
            {
                address.Protocols.AddRange(new MultiAddress("/udp/" + actualPort).Protocols);
            }
        }

        // TODO: UDP listener
        throw new NotImplementedException();
#if false
            var stream = new DatagramStream(socket, ownsSocket: true);
            handler(stream, address, null);

            return address;
#endif
    }
}