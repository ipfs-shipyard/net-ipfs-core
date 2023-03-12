using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi;

internal class GenericApi : IGenericApi
{
    private readonly IpfsEngine _ipfs;

    public GenericApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task<Peer> IdAsync(MultiHash peer = null, CancellationToken cancel = default)
    {
        if (peer == null)
        {
            return await _ipfs.LocalPeer.ConfigureAwait(false);
        }

        return await _ipfs.Dht.FindPeerAsync(peer, cancel).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PingResult>> PingAsync(MultiHash peer, int count = 10,
        CancellationToken cancel = default)
    {
        var ping = await _ipfs.PingService;
        return await ping.PingAsync(peer, count, cancel);
    }

    public async Task<IEnumerable<PingResult>> PingAsync(MultiAddress address, int count = 10,
        CancellationToken cancel = default)
    {
        var ping = await _ipfs.PingService;
        return await ping.PingAsync(address, count, cancel);
    }

    public async Task<string> ResolveAsync(string name, bool recursive = true, CancellationToken cancel = default)
    {
        var path = name;
        if (path.StartsWith("/ipns/"))
        {
            path = await _ipfs.Name.ResolveAsync(path, recursive, false, cancel).ConfigureAwait(false);
            if (!recursive)
            {
                return path;
            }
        }

        if (path.StartsWith("/ipfs/"))
        {
            path = path.Remove(0, 6);
        }

        var parts = path.Split('/').Where(p => p.Length > 0).ToArray();
        if (parts.Length == 0)
        {
            throw new ArgumentException($"Cannot resolve '{name}'.");
        }

        var id = Cid.Decode(parts[0]);
        foreach (var child in parts.Skip(1))
        {
            var container = await _ipfs.Object.GetAsync(id, cancel).ConfigureAwait(false);
            var link = container.Links.FirstOrDefault(l => l.Name == child);
            id = link == null ? throw new ArgumentException($"Cannot resolve '{child}' in '{name}'.") : link.Id;
        }

        return "/ipfs/" + id.Encode();
    }

    public Task ShutdownAsync()
    {
        return _ipfs.StopAsync();
    }

    public async Task<Dictionary<string, string>> VersionAsync(CancellationToken cancel = default)
    {
        var version = typeof(GenericApi).GetTypeInfo().Assembly.GetName().Version;
        return new()
        {
            { "Version", $"{version?.Major}.{version?.Minor}.{version?.Revision}" },
            { "Repo", await _ipfs.BlockRepository.VersionAsync(cancel) }
        };
    }
}