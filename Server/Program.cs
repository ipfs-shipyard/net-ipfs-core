using System;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Engine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace IpfsShipyard.Ipfs.Server;

/// <summary>
///     Manages an IPFS server.
/// </summary>
public class Program
{
    private const string Passphrase = "this is not a secure pass phrase";
    private static readonly CancellationTokenSource Cancel = new();

    /// <summary>
    ///     The IPFS Core API engine.
    /// </summary>
    public static IpfsEngine IpfsEngine;

    /// <summary>
    ///     Main entry point.
    /// </summary>
    public static void Main(string[] args)
    {
        try
        {
            IpfsEngine = new(Passphrase);
            IpfsEngine.StartAsync().Wait();

            BuildWebHost(args)
                .RunAsync(Cancel.Token)
                .Wait();
        }
        catch (TaskCanceledException)
        {
            // eat it
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message); // TODO: better error handling
        }

        IpfsEngine?.StopAsync().Wait();
    }

    private static IWebHost BuildWebHost(string[] args)
    {
        var urls = "http://127.0.0.1:5009";
        var addr = (string)IpfsEngine.Config.GetAsync("Addresses.API").Result;
        if (addr != null)
        {
            // Quick and dirty: multiaddress to URL
            urls = addr
                .Replace("/ip4/", "http://")
                .Replace("/ip6/", "http://")
                .Replace("/tcp/", ":");
        }

        return WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureLogging(logging => { logging.ClearProviders(); })
            .UseUrls(urls)
            .Build();
    }

    /// <summary>
    ///     Stop the program.
    /// </summary>
    public static void Shutdown()
    {
        Cancel.Cancel();
    }
}