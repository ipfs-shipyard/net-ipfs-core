using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Ipfs.Engine.UnixFileSystem;
using ProtoBuf;

namespace Ipfs.Engine.CoreApi;

internal class ObjectApi : IObjectApi
{
    internal static DagNode EmptyNode;
    internal static DagNode EmptyDirectory;

    private readonly IpfsEngine _ipfs;

    static ObjectApi()
    {
        EmptyNode = new(Array.Empty<byte>());
        var _ = EmptyNode.Id;

        var dm = new DataMessage { Type = DataType.Directory };
        using var pb = new MemoryStream();
        Serializer.Serialize(pb, dm);
        EmptyDirectory = new(pb.ToArray());
    }

    public ObjectApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task<Stream> DataAsync(Cid id, CancellationToken cancel = default)
    {
        var node = await GetAsync(id, cancel).ConfigureAwait(false);
        return node.DataStream;
    }

    public async Task<DagNode> GetAsync(Cid id, CancellationToken cancel = default)
    {
        var block = await _ipfs.Block.GetAsync(id, cancel).ConfigureAwait(false);
        return new(block.DataStream);
    }

    public async Task<IEnumerable<IMerkleLink>> LinksAsync(Cid id, CancellationToken cancel = default)
    {
        if (id.ContentType != "dag-pb")
        {
            return Enumerable.Empty<IMerkleLink>();
        }

        var block = await _ipfs.Block.GetAsync(id, cancel).ConfigureAwait(false);
        var node = new DagNode(block.DataStream);
        return node.Links;
    }

    public Task<DagNode> NewAsync(string template = null, CancellationToken cancel = default)
    {
        return template switch
        {
            null => Task.FromResult(EmptyNode),
            "unixfs-dir" => Task.FromResult(EmptyDirectory),
            _ => throw new ArgumentException($"Unknown template '{template}'.", nameof(template))
        };
    }

    public Task<DagNode> NewDirectoryAsync(CancellationToken cancel = default)
    {
        return Task.FromResult(EmptyDirectory);
    }

    public Task<DagNode> PutAsync(byte[] data, IEnumerable<IMerkleLink> links = null,
        CancellationToken cancel = default)
    {
        var node = new DagNode(data, links);
        return PutAsync(node, cancel);
    }

    public async Task<DagNode> PutAsync(DagNode node, CancellationToken cancel = default)
    {
        node.Id = await _ipfs.Block.PutAsync(node.ToArray(), cancel: cancel).ConfigureAwait(false);
        return node;
    }

    public async Task<ObjectStat> StatAsync(Cid id, CancellationToken cancel = default)
    {
        var block = await _ipfs.Block.GetAsync(id, cancel).ConfigureAwait(false);
        var node = new DagNode(block.DataStream);
        return new()
        {
            BlockSize = block.Size,
            DataSize = node.DataBytes.Length,
            LinkCount = node.Links.Count(),
            LinkSize = block.Size - node.DataBytes.Length,
            CumulativeSize = block.Size + node.Links.Sum(link => link.Size)
        };
    }
}