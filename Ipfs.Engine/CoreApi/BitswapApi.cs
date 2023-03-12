using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;

namespace Ipfs.Engine.CoreApi;

internal class BitswapApi : IBitswapApi
{
    private readonly IpfsEngine _ipfs;

    public BitswapApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default)
    {
        var bs = await _ipfs.BitswapService.ConfigureAwait(false);
        var peer = await _ipfs.LocalPeer.ConfigureAwait(false);
        return await bs.WantAsync(id, peer.Id, cancel).ConfigureAwait(false);
    }

    public async Task<BitswapLedger> LedgerAsync(Peer peer, CancellationToken cancel = default)
    {
        var bs = await _ipfs.BitswapService.ConfigureAwait(false);
        return bs.PeerLedger(peer);
    }

    public async Task UnwantAsync(Cid id, CancellationToken cancel = default)
    {
        (await _ipfs.BitswapService.ConfigureAwait(false)).Unwant(id);
    }

    public async Task<IEnumerable<Cid>> WantsAsync(MultiHash peer = null, CancellationToken cancel = default)
    {
        if (peer == null)
        {
            peer = (await _ipfs.LocalPeer.ConfigureAwait(false)).Id;
        }

        var bs = await _ipfs.BitswapService.ConfigureAwait(false);
        return bs.PeerWants(peer);
    }
}