using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.CoreApi;

internal class StatApi : IStatsApi
{
    private readonly IpfsClient _ipfs;

    internal StatApi(IpfsClient ipfs)
    {
        _ipfs = ipfs;
    }

    public Task<BandwidthData> BandwidthAsync(CancellationToken cancel = default)
    {
        return _ipfs.DoCommandAsync<BandwidthData>("stats/bw", cancel);
    }

    public async Task<BitswapData> BitswapAsync(CancellationToken cancel = default)
    {
        var json = await _ipfs.DoCommandAsync("stats/bitswap", cancel);
        var stat = JObject.Parse(json);
        return new()
        {
            BlocksReceived = (ulong)stat["BlocksReceived"],
            DataReceived = (ulong)stat["DataReceived"],
            BlocksSent = (ulong)stat["BlocksSent"],
            DataSent = (ulong)stat["DataSent"],
            DupBlksReceived = (ulong)stat["DupBlksReceived"],
            DupDataReceived = (ulong)stat["DupDataReceived"],
            ProvideBufLen = (int)stat["ProvideBufLen"],
            Peers = ((JArray)stat["Peers"]).Select(s => new MultiHash((string)s)),
            Wantlist = ((JArray)stat["Wantlist"]).Select(o => Cid.Decode(o["/"].ToString()))
        };
    }

    public Task<RepositoryData> RepositoryAsync(CancellationToken cancel = default)
    {
        return _ipfs.DoCommandAsync<RepositoryData>("stats/repo", cancel);
    }


}