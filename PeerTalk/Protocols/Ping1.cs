using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Semver;

namespace IpfsShipyard.PeerTalk.Protocols;

/// <summary>
///   Ping Protocol version 1.0
/// </summary>
public class Ping1 : IPeerProtocol, IService
{
    private const int PingSize = 32;

    private static readonly ILog log = LogManager.GetLogger(typeof(Ping1));

    /// <inheritdoc />
    public string Name { get; } = "ipfs/ping";

    /// <inheritdoc />
    public SemVersion Version { get; } = new(1, 0);

    /// <summary>
    ///   Provides access to other peers.
    /// </summary>
    public Swarm Swarm { get; set; }

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
            // Read the message.
            var request = new byte[PingSize];
            await stream.ReadExactAsync(request, 0, PingSize, cancel).ConfigureAwait(false);
            log.Debug($"got ping from {connection.RemotePeer}");

            // Echo the message
            await stream.WriteAsync(request, 0, PingSize, cancel).ConfigureAwait(false);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task StartAsync()
    {
        log.Debug("Starting");

        Swarm.AddProtocol(this);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        log.Debug("Stopping");

        Swarm.RemoveProtocol(this);

        return Task.CompletedTask;
    }

    /// <summary>
    ///   Send echo requests to a peer.
    /// </summary>
    /// <param name="peerId">
    ///   The peer ID to receive the echo requests.
    /// </param>
    /// <param name="count">
    ///   The number of echo requests to send.  Defaults to 10.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task's value is
    ///   the sequence of <see cref="PingResult"/>.
    /// </returns>
    public async Task<IEnumerable<PingResult>> PingAsync(MultiHash peerId, int count = 10, CancellationToken cancel = default)
    {
        var peer = new Peer { Id = peerId };
        return await PingAsync(peer, count, cancel).ConfigureAwait(false);
    }

    /// <summary>
    ///   Send echo requests to a peer.
    /// </summary>
    /// <param name="address">
    ///   The address of a peer to receive the echo requests.
    /// </param>
    /// <param name="count">
    ///   The number of echo requests to send.  Defaults to 10.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task's value is
    ///   the sequence of <see cref="PingResult"/>.
    /// </returns>
    public async Task<IEnumerable<PingResult>> PingAsync(MultiAddress address, int count = 10, CancellationToken cancel = default)
    {
        var peer = Swarm.RegisterPeerAddress(address);
        return await PingAsync(peer, count, cancel).ConfigureAwait(false);
    }

    private async Task<IEnumerable<PingResult>> PingAsync(Peer peer, int count, CancellationToken cancel)
    {
        var ping = new byte[PingSize];
        var rng = new Random();
        var results = new List<PingResult>
        {
            new() { Success = true, Text = $"PING {peer}."}
        };
        var totalTime = TimeSpan.Zero;

        await using (var stream = await Swarm.DialAsync(peer, ToString(), cancel))
        {
            for (var i = 0; i < count; ++i)
            {
                rng.NextBytes(ping);

                var start = DateTime.Now;
                try
                {
                    await stream.WriteAsync(ping, 0, ping.Length).ConfigureAwait(false); ;
                    await stream.FlushAsync(cancel).ConfigureAwait(false);

                    var response = new byte[PingSize];
                    await stream.ReadExactAsync(response, 0, PingSize, cancel).ConfigureAwait(false);

                    var result = new PingResult
                    {
                        Time = DateTime.Now - start,
                    };
                    totalTime += result.Time;
                    if (ping.SequenceEqual(response))
                    {
                        result.Success = true;
                        result.Text = "";
                    }
                    else
                    {
                        result.Success = false;
                        result.Text = "ping packet was incorrect!";
                    }

                    results.Add(result);
                }
                catch (Exception e)
                {
                    results.Add(new()
                    {
                        Success = false,
                        Time = DateTime.Now - start,
                        Text = e.Message
                    });
                }
            }
        }

        var avg = totalTime.TotalMilliseconds / count;
        results.Add(new()
        {
            Success = true,
            Text = $"Average latency: {avg:0.000}ms"
        });

        return results;
    }
}

internal class PingMessage
{
}