using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.CoreApi;

internal class DhtApi : IDhtApi
{
    private readonly IpfsClient _ipfs;

    internal DhtApi(IpfsClient ipfs)
    {
        _ipfs = ipfs;
    }

    public Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default)
    {
        return _ipfs.IdAsync(id, cancel);
    }

    public async Task<IEnumerable<Peer>> FindProvidersAsync(Cid id, int limit = 20, Action<Peer> providerFound = null, CancellationToken cancel = default)
    {
        // TODO: providerFound action
        var stream = await _ipfs.PostDownloadAsync("dht/findprovs", cancel, id, $"num-providers={limit}");
        return ProviderFromStream(stream, limit);
    }

    public Task<byte[]> GetAsync(byte[] key, CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }

    public Task ProvideAsync(Cid cid, bool advertise = true, CancellationToken cancel = default)
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

    private IEnumerable<Peer> ProviderFromStream(Stream stream, int limit = int.MaxValue)
    {
        using var sr = new StreamReader(stream);
        var n = 0;
        while (!sr.EndOfStream && n < limit)
        {
            var json = sr.ReadLine();

            var r = JObject.Parse(json);
            var id = (string)r["ID"];
            if (id != string.Empty)
            {
                ++n;
                yield return new() { Id = new(id) };
            }
            else
            {
                var responses = (JArray)r["Responses"];
                if (responses != null)
                {
                    foreach (var rid in responses.Select(response => (string)response["ID"]).Where(rid => rid != string.Empty))
                    {
                        ++n;
                        yield return new() { Id = new(rid) };
                    }
                }
            }
        }
    }
}