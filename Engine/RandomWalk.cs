using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using IpfsShipyard.PeerTalk;

namespace IpfsShipyard.Ipfs.Engine;

/// <summary>
///     Periodically queries the DHT to discover new peers.
/// </summary>
/// <remarks>
///     A background task is created to query the DHT.  It is designed
///     to run often at startup and then less often at time increases.
/// </remarks>
public class RandomWalk : IService
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(RandomWalk));
    private CancellationTokenSource _cancel;

    /// <summary>
    ///     The time to wait until running the query.
    /// </summary>
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The time to add to the <see cref="Delay" />.
    /// </summary>
    public TimeSpan DelayIncrement = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     The maximum <see cref="Delay" />.
    /// </summary>
    public TimeSpan DelayMax = TimeSpan.FromMinutes(9);

    /// <summary>
    ///     The Distributed Hash Table to query.
    /// </summary>
    public IDhtApi Dht { get; set; }

    /// <summary>
    ///     Start a background process that will run a random
    ///     walk every <see cref="Delay" />.
    /// </summary>
    public Task StartAsync()
    {
        if (_cancel != null)
        {
            throw new("Already started.");
        }

        _cancel = new();
        _ = RunnerAsync(_cancel.Token);

        Log.Debug("started");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Stop the background process.
    /// </summary>
    public Task StopAsync()
    {
        _cancel?.Cancel();
        _cancel?.Dispose();
        _cancel = null;

        Log.Debug("stopped");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     The background process.
    /// </summary>
    private async Task RunnerAsync(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Delay, cancellation);
                await RunQueryAsync(cancellation).ConfigureAwait(false);
                Log.Debug("query finished");
                Delay += DelayIncrement;
                if (Delay > DelayMax)
                {
                    Delay = DelayMax;
                }
            }
            catch (TaskCanceledException)
            {
                // eat it.
            }
            catch (Exception e)
            {
                Log.Error("run query failed", e);
                // eat all exceptions
            }
        }
    }

    private async Task RunQueryAsync(CancellationToken cancel = default)
    {
        // Tests may not set a DHT.
        if (Dht == null)
        {
            return;
        }

        Log.Debug("Running a query");

        // Get a random peer id.
        var x = new byte[32];
        var rng = new Random();
        rng.NextBytes(x);
        var id = MultiHash.ComputeHash(x);

        await Dht.FindPeerAsync(id, cancel).ConfigureAwait(false);
    }
}