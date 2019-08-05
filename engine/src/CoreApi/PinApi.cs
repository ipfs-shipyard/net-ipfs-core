﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class Pin
    {
        public Cid Id;
    }

    class PinApi : IPinApi
    {
        IpfsEngine ipfs;
        FileStore<Cid, Pin> store;

        public PinApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        FileStore<Cid, Pin> Store
        {
            get
            {
                if (store == null)
                {
                    var folder = Path.Combine(ipfs.Options.Repository.Folder, "pins");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    store = new FileStore<Cid, Pin>
                    {
                        Folder = folder,
                        NameToKey = (cid) => cid.Hash.ToBase32(),
                        KeyToName = (key) => new MultiHash(key.FromBase32())
                    };
                }
                return store;
            }
        }

        public async Task<IEnumerable<Cid>> AddAsync(string path, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            var id = await ipfs.ResolveIpfsPathToCidAsync(path, cancel).ConfigureAwait(false);
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
                await Store.PutAsync(current, new Pin { Id = current }, cancel).ConfigureAwait(false);

                // Make sure that the content is stored locally.
                await ipfs.Block.GetAsync(current, cancel).ConfigureAwait(false);

                // Recursively pin the links?
                if (recursive && current.ContentType == "dag-pb")
                {
                    var links = await ipfs.Object.LinksAsync(current, cancel);
                    foreach (var link in links)
                    {
                        todos.Push(link.Id);
                    }
                }

                dones.Add(current);
            }

            return dones;
        }

        public Task<IEnumerable<Cid>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            var cids = Store.Values
                .Select(pin => pin.Id);
            return Task.FromResult(cids);
        }

        public async Task<IEnumerable<Cid>> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default(CancellationToken))
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
                    if (null != await ipfs.Block.StatAsync(current, cancel).ConfigureAwait(false))
                    {
                        try
                        {
                            var links = await ipfs.Object.LinksAsync(current, cancel).ConfigureAwait(false);
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

        public async Task<bool> IsPinnedAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            return await Store.ExistsAsync(id, cancel).ConfigureAwait(false);
        }
    }
}
