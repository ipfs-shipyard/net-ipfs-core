using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;

namespace Ipfs.Engine.CoreApi;

internal class DhtApi : IDhtApi
{
    private readonly IpfsEngine _ipfs;

    public DhtApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default)
    {
        var dht = await _ipfs.DhtService.ConfigureAwait(false);
        return await dht.FindPeerAsync(id, cancel).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Peer>> FindProvidersAsync(Cid id, int limit = 20, Action<Peer> providerFound = null,
        CancellationToken cancel = default)
    {
        var dht = await _ipfs.DhtService.ConfigureAwait(false);
        return await dht.FindProvidersAsync(id, limit, providerFound, cancel).ConfigureAwait(false);
    }

    public async Task ProvideAsync(Cid cid, bool advertise = true, CancellationToken cancel = default)
    {
        var dht = await _ipfs.DhtService.ConfigureAwait(false);
        await dht.ProvideAsync(cid, advertise, cancel).ConfigureAwait(false);
    }

    public Task<byte[]> GetAsync(byte[] key, CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }

    public Task PutAsync(byte[] key, out byte[] value, CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryGetAsync(byte[] key, out byte[] value, CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }
}