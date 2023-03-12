using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;

namespace Ipfs.Engine.CoreApi;

internal class Pin
{
    public Cid Id;
}

internal class PinApi : IPinApi
{
    private readonly IpfsEngine _ipfs;
    private FileStore<Cid, Pin> _store;

    public PinApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    private FileStore<Cid, Pin> Store
    {
        get
        {
            if (_store != null)
            {
                return _store;
            }

            var folder = Path.Combine(_ipfs.Options.Repository.Folder, "pins");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _store = new()
            {
                Folder = folder,
                NameToKey = cid => cid.Hash.ToBase32(),
                KeyToName = key => new MultiHash(key.FromBase32())
            };

            return _store;
        }
    }

    public async Task<IEnumerable<Cid>> AddAsync(string path, bool recursive = true, CancellationToken cancel = default)
    {
        var id = await _ipfs.ResolveIpfsPathToCidAsync(path, cancel).ConfigureAwait(false);
        var todos = new Stack<Cid>();
        todos.Push(id);
        var dones = new List<Cid>();

        // The pin is added before the content is fetched, so that
        // garbage collection will not delete the newly pinned
        // content.

        while (todos.Count > 0)
        {
            var current = todos.Pop();

            // Add CID to PIN database.
            await Store.PutAsync(current, new() { Id = current }, cancel).ConfigureAwait(false);

            // Make sure that the content is stored locally.
            await _ipfs.Block.GetAsync(current, cancel).ConfigureAwait(false);

            // Recursively pin the links?
            if (recursive && current.ContentType == "dag-pb")
            {
                var links = await _ipfs.Object.LinksAsync(current, cancel);
                foreach (var link in links)
                {
                    todos.Push(link.Id);
                }
            }

            dones.Add(current);
        }

        return dones;
    }

    public Task<IEnumerable<Cid>> ListAsync(CancellationToken cancel = default)
    {
        var cids = Store.Values
            .Select(pin => pin.Id);
        return Task.FromResult(cids);
    }

    public async Task<IEnumerable<Cid>> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default)
    {
        var todos = new Stack<Cid>();
        todos.Push(id);
        var dones = new List<Cid>();

        while (todos.Count > 0)
        {
            var current = todos.Pop();
            await Store.RemoveAsync(current, cancel).ConfigureAwait(false);
            if (recursive)
            {
                if (null != await _ipfs.Block.StatAsync(current, cancel).ConfigureAwait(false))
                {
                    try
                    {
                        var links = await _ipfs.Object.LinksAsync(current, cancel).ConfigureAwait(false);
                        foreach (var link in links)
                        {
                            todos.Push(link.Id);
                        }
                    }
                    catch (Exception)
                    {
                        // ignore if current is not an objcet.
                    }
                }
            }

            dones.Add(current);
        }

        return dones;
    }

    public async Task<bool> IsPinnedAsync(Cid id, CancellationToken cancel = default)
    {
        return await Store.ExistsAsync(id, cancel).ConfigureAwait(false);
    }
}