using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.CoreApi;

internal class ConfigApi : IConfigApi
{
    private readonly IpfsClient _ipfs;

    internal ConfigApi(IpfsClient ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task<JObject> GetAsync(CancellationToken cancel = default)
    {
        var json = await _ipfs.DoCommandAsync("config/show", cancel);
        return JObject.Parse(json);
    }

    public async Task<JToken> GetAsync(string key, CancellationToken cancel = default)
    {
        var json = await _ipfs.DoCommandAsync("config", cancel, key);
        var r = JObject.Parse(json);
        return r["Value"];
    }

    public async Task SetAsync(string key, string value, CancellationToken cancel = default)
    {
        var _ = await _ipfs.DoCommandAsync("config", cancel, key, "arg=" + value);
    }

    public async Task SetAsync(string key, JToken value, CancellationToken cancel = default)
    {
        var _ = await _ipfs.DoCommandAsync("config", cancel,
            key,
            "arg=" + value.ToString(Formatting.None),
            "json=true");
    }

    public async Task ReplaceAsync(JObject config)
    {
        var data = Encoding.UTF8.GetBytes(config.ToString(Formatting.None));
        await _ipfs.UploadAsync("config/replace", CancellationToken.None, data);
    }
}