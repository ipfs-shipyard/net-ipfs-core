using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.CoreApi;

internal class BlockRepositoryApi : IBlockRepositoryApi
{
    private readonly IpfsClient _ipfs;

    internal BlockRepositoryApi(IpfsClient ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task RemoveGarbageAsync(CancellationToken cancel = default)
    {
        await _ipfs.DoCommandAsync("repo/gc", cancel);
    }

    public Task<RepositoryData> StatisticsAsync(CancellationToken cancel = default)
    {
        return _ipfs.DoCommandAsync<RepositoryData>("repo/stat", cancel);
    }

    public async Task VerifyAsync(CancellationToken cancel = default)
    {
        await _ipfs.DoCommandAsync("repo/verify", cancel);
    }

    public async Task<string> VersionAsync(CancellationToken cancel = default)
    {
        var json = await _ipfs.DoCommandAsync("repo/version", cancel);
        var info = JObject.Parse(json);
        return (string)info["Version"];
    }
}