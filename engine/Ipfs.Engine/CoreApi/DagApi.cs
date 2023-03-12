using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Ipfs.Engine.LinkedData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterO.Cbor;

namespace Ipfs.Engine.CoreApi;

internal class DagApi : IDagApi
{
    private static readonly PODOptions PodOptions = new("usecamelcase=false");
    private readonly IpfsEngine _ipfs;

    public DagApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task<JObject> GetAsync(
        Cid id,
        CancellationToken cancel = default)
    {
        var block = await _ipfs.Block.GetAsync(id, cancel).ConfigureAwait(false);
        var format = GetDataFormat(id);
        var canonical = format.Deserialise(block.DataBytes);
        using var ms = new MemoryStream();
        using var sr = new StreamReader(ms);
        await using var reader = new JsonTextReader(sr);
        canonical.WriteJSONTo(ms);
        ms.Position = 0;
        return (JObject)await JToken.ReadFromAsync(reader, cancel);
    }

    public async Task<JToken> GetAsync(
        string path,
        CancellationToken cancel = default)
    {
        if (path.StartsWith("/ipfs/"))
        {
            path = path.Remove(0, 6);
        }

        var parts = path.Split('/').Where(p => p.Length > 0).ToArray();
        if (parts.Length == 0)
        {
            throw new ArgumentException($"Cannot resolve '{path}'.");
        }

        JToken token = await GetAsync(Cid.Decode(parts[0]), cancel).ConfigureAwait(false);
        foreach (var child in parts.Skip(1))
        {
            token = ((JObject)token)[child];
            if (token == null)
            {
                throw new($"Missing component '{child}'.");
            }
        }

        return token;
    }

    public async Task<T> GetAsync<T>(
        Cid id,
        CancellationToken cancel = default)
    {
        var block = await _ipfs.Block.GetAsync(id, cancel).ConfigureAwait(false);
        var format = GetDataFormat(id);
        var canonical = format.Deserialise(block.DataBytes);

        // CBOR does not support serialisation to another Type
        // see https://github.com/peteroupc/CBOR/issues/12.
        // So, convert to JSON and use Newtonsoft to deserialise.
        return JObject
            .Parse(canonical.ToJSONString())
            .ToObject<T>();
    }

    public async Task<Cid> PutAsync(
        JObject data,
        string contentType = "dag-cbor",
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = true,
        CancellationToken cancel = default)
    {
        using var ms = new MemoryStream();
        await using var sw = new StreamWriter(ms);
        await using var writer = new JsonTextWriter(sw);
        await data.WriteToAsync(writer, cancel);
        await writer.FlushAsync(cancel);
        ms.Position = 0;
        var format = GetDataFormat(contentType);
        var block = format.Serialize(CBORObject.ReadJSON(ms));
        return await _ipfs.Block.PutAsync(block, contentType, multiHash, encoding, pin, cancel).ConfigureAwait(false);
    }

    public async Task<Cid> PutAsync(Stream data,
        string contentType = "dag-cbor",
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = true,
        CancellationToken cancel = default)
    {
        var format = GetDataFormat(contentType);
        var block = format.Serialize(CBORObject.Read(data));
        return await _ipfs.Block.PutAsync(block, contentType, multiHash, encoding, pin, cancel).ConfigureAwait(false);
    }

    public async Task<Cid> PutAsync(object data,
        string contentType = "dag-cbor",
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = true,
        CancellationToken cancel = default)
    {
        var format = GetDataFormat(contentType);
        var block = format.Serialize(CBORObject.FromObject(data, PodOptions));
        return await _ipfs.Block.PutAsync(block, contentType, multiHash, encoding, pin, cancel).ConfigureAwait(false);
    }

    private static ILinkedDataFormat GetDataFormat(Cid id) =>
        IpldRegistry.Formats.TryGetValue(id.ContentType, out var format)
            ? format
            : throw new KeyNotFoundException($"Unknown IPLD format '{id.ContentType}'.");

    private static ILinkedDataFormat GetDataFormat(string contentType) =>
        IpldRegistry.Formats.TryGetValue(contentType, out var format)
            ? format
            : throw new KeyNotFoundException($"Unknown IPLD format '{contentType}'.");
}