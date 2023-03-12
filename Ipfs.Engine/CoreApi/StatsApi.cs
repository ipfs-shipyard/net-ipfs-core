using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core.CoreApi;
using PeerTalk;

namespace Ipfs.Engine.CoreApi;

internal class StatsApi : IStatsApi
{
    private readonly IpfsEngine _ipfs;

    public StatsApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public Task<BandwidthData> BandwidthAsync(CancellationToken cancel = default) => Task.FromResult(StatsStream.AllBandwidth);

    public async Task<BitswapData> BitswapAsync(CancellationToken cancel = default)
    {
        var bitswap = await _ipfs.BitswapService.ConfigureAwait(false);
        return bitswap.Statistics;
    }

    public Task<RepositoryData> RepositoryAsync(CancellationToken cancel = default) => _ipfs.BlockRepository.StatisticsAsync(cancel);
}