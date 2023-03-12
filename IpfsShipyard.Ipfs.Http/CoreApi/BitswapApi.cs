using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.CoreApi;

class BitswapApi : IBitswapApi
{
    readonly IpfsClient _ipfs;

    internal BitswapApi(IpfsClient ipfs)
    {
        _ipfs = ipfs;
    }

    public Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
    {
        return _ipfs.Block.GetAsync(id, cancel);
    }

    public async Task<IEnumerable<Cid>> WantsAsync(MultiHash peer = null, CancellationToken cancel = default(CancellationToken))
    {
        var json = await _ipfs.DoCommandAsync("bitswap/wantlist", cancel, peer?.ToString());
        var keys = (JArray)(JObject.Parse(json)["Keys"]);
        // https://github.com/ipfs/go-ipfs/issues/5077
        return keys
            .Select(k =>
            {
                if (k.Type == JTokenType.String)
                    return Cid.Decode(k.ToString());
                var obj = (JObject)k;
                return Cid.Decode(obj["/"].ToString());
            });
    }

    public async Task UnwantAsync(Cid id, CancellationToken cancel = default(CancellationToken))
    {
        await _ipfs.DoCommandAsync("bitswap/unwant", cancel, id);
    }

    public async Task<BitswapLedger> LedgerAsync(Peer peer, CancellationToken cancel = default(CancellationToken))
    {
        var json = await _ipfs.DoCommandAsync("bitswap/ledger", cancel, peer.Id.ToString());
        var o = JObject.Parse(json);
        return new BitswapLedger
        {
            Peer = (string)o["Peer"],
            DataReceived = (ulong)o["Sent"],
            DataSent = (ulong)o["Recv"],
            BlocksExchanged = (ulong)o["Exchanged"]
        };
    }
}