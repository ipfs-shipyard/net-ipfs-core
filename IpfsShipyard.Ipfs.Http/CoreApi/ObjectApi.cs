using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.CoreApi;

internal class ObjectApi : IObjectApi
{
    private readonly IpfsClient _ipfs;

    internal ObjectApi(IpfsClient ipfs)
    {
        _ipfs = ipfs;
    }

    public Task<DagNode> NewDirectoryAsync(CancellationToken cancel = default)
    {
        return NewAsync("unixfs-dir", cancel);
    }

    public async Task<DagNode> NewAsync(string template = null, CancellationToken cancel = default)
    {
        var json = await _ipfs.DoCommandAsync("object/new", cancel, template);
        var hash = (string)JObject.Parse(json)["Hash"];
        return await GetAsync(hash);
    }

    public async Task<DagNode> GetAsync(Cid id, CancellationToken cancel = default)
    {
        var json = await _ipfs.DoCommandAsync("object/get", cancel, id);
        return GetDagFromJson(json);
    }

    public Task<DagNode> PutAsync(byte[] data, IEnumerable<IMerkleLink> links = null, CancellationToken cancel = default)
    {
        return PutAsync(new(data, links), cancel);
    }

    public async Task<DagNode> PutAsync(DagNode node, CancellationToken cancel = default)
    {
        var json = await _ipfs.UploadAsync("object/put", cancel, node.ToArray(), "inputenc=protobuf");
        return node;
    }

    public Task<Stream> DataAsync(Cid id, CancellationToken cancel = default)
    {
        return _ipfs.PostDownloadAsync("object/data", cancel, id);
    }

    public async Task<IEnumerable<IMerkleLink>> LinksAsync(Cid id, CancellationToken cancel = default)
    {
        var json = await _ipfs.DoCommandAsync("object/links", cancel, id);
        return GetDagFromJson(json).Links;
    }

    // TOOD: patch sub API

    private DagNode GetDagFromJson(string json)
    {
        var result = JObject.Parse(json);
        byte[] data = null;
        var stringData = (string)result["Data"];
        if (stringData != null)
            data = Encoding.UTF8.GetBytes(stringData);
        var links = ((JArray)result["Links"])
            .Select(link => new DagLink(
                (string)link["Name"],
                (string)link["Hash"],
                (long)link["Size"]));
        return new(data, links);
    }

    public async Task<ObjectStat> StatAsync(Cid id, CancellationToken cancel = default)
    {
        var json = await _ipfs.DoCommandAsync("object/stat", cancel, id);
        var r = JObject.Parse(json);

        return new()
        {
            LinkCount = (int)r["NumLinks"],
            LinkSize = (long)r["LinksSize"],
            BlockSize = (long)r["BlockSize"],
            DataSize = (long)r["DataSize"],
            CumulativeSize = (long)r["CumulativeSize"]
        };
    }
}