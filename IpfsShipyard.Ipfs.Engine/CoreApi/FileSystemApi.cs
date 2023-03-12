using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using ICSharpCode.SharpZipLib.Tar;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using IpfsShipyard.Ipfs.Engine.UnixFileSystem;
using ProtoBuf;

namespace IpfsShipyard.Ipfs.Engine.CoreApi;

internal class FileSystemApi : IFileSystemApi
{
    private static ILog _log = LogManager.GetLogger(typeof(FileSystemApi));

    private static readonly int DefaultLinksPerBlock = 174;
    private readonly IpfsEngine _ipfs;

    public FileSystemApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task<IFileSystemNode> AddFileAsync(
        string path,
        AddFileOptions options = default,
        CancellationToken cancel = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await AddAsync(stream, Path.GetFileName(path), options, cancel).ConfigureAwait(false);
    }

    public async Task<IFileSystemNode> AddTextAsync(
        string text,
        AddFileOptions options = default,
        CancellationToken cancel = default)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text), false);
        return await AddAsync(ms, "", options, cancel).ConfigureAwait(false);
    }

    public async Task<IFileSystemNode> AddAsync(
        Stream stream,
        string name,
        AddFileOptions options,
        CancellationToken cancel)
    {
        options ??= new();

        // TODO: various options
        if (options.Trickle)
        {
            throw new NotImplementedException("Trickle");
        }

        var blockService = GetBlockService(options);
        var keyChain = await _ipfs.KeyChainAsync(cancel).ConfigureAwait(false);

        var chunker = new SizeChunker();
        var nodes = await chunker.ChunkAsync(stream, name, options, blockService, keyChain, cancel)
            .ConfigureAwait(false);

        // Multiple nodes for the file?
        var node = await BuildTreeAsync(nodes, options, cancel);

        // Wrap in directory?
        if (options.Wrap)
        {
            var link = node.ToLink(name);
            var wlinks = new[] { link };
            node = await CreateDirectoryAsync(wlinks, options, cancel).ConfigureAwait(false);
        }
        else
        {
            node.Name = name;
        }

        // Advertise the root node.
        if (options.Pin && _ipfs.IsStarted)
        {
            await _ipfs.Dht.ProvideAsync(node.Id, true, cancel).ConfigureAwait(false);
        }

        // Return the file system node.
        return node;
    }

    public async Task<IFileSystemNode> AddDirectoryAsync(
        string path,
        bool recursive = true,
        AddFileOptions options = default,
        CancellationToken cancel = default)
    {
        options ??= new();
        options.Wrap = false;

        // Add the files and sub-directories.
        path = Path.GetFullPath(path);
        var files = Directory
            .EnumerateFiles(path)
            .OrderBy(s => s)
            .Select(p => AddFileAsync(p, options, cancel));
        if (recursive)
        {
            var folders = Directory
                .EnumerateDirectories(path)
                .OrderBy(s => s)
                .Select(dir => AddDirectoryAsync(dir, true, options, cancel));
            files = files.Union(folders);
        }

        var nodes = await Task.WhenAll(files).ConfigureAwait(false);

        // Create the DAG with links to the created files and sub-directories
        var links = nodes
            .Select(node => node.ToLink())
            .ToArray();
        var fsn = await CreateDirectoryAsync(links, options, cancel).ConfigureAwait(false);
        fsn.Name = Path.GetFileName(path);
        return fsn;
    }

    public async Task<IFileSystemNode> ListFileAsync(string path, CancellationToken cancel = default)
    {
        var cid = await _ipfs.ResolveIpfsPathToCidAsync(path, cancel).ConfigureAwait(false);
        var block = await _ipfs.Block.GetAsync(cid, cancel).ConfigureAwait(false);

        switch (cid.ContentType)
        {
            // TODO: A content-type registry should be used.
            case "dag-pb":
                // fall thru
                break;
            case "raw":
                return new FileSystemNode
                {
                    Id = cid,
                    Size = block.Size
                };
            case "cms":
                return new FileSystemNode
                {
                    Id = cid,
                    Size = block.Size
                };
            default:
                throw new NotSupportedException($"Cannot read content type '{cid.ContentType}'.");
        }

        var dag = new DagNode(block.DataStream);
        var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
        var fsn = new FileSystemNode
        {
            Id = cid,
            Links = dag.Links
                .Select(l => new FileSystemLink
                {
                    Id = l.Id,
                    Name = l.Name,
                    Size = l.Size
                })
                .ToArray(),
            IsDirectory = dm.Type == DataType.Directory,
            Size = (long)(dm.FileSize ?? 0)
        };


        return fsn;
    }

    public async Task<string> ReadAllTextAsync(string path, CancellationToken cancel = default)
    {
        await using var data = await ReadFileAsync(path, cancel).ConfigureAwait(false);
        using var text = new StreamReader(data);
        return await text.ReadToEndAsync(cancel).ConfigureAwait(false);
    }

    public async Task<Stream> ReadFileAsync(string path, CancellationToken cancel = default)
    {
        var cid = await _ipfs.ResolveIpfsPathToCidAsync(path, cancel).ConfigureAwait(false);
        var keyChain = await _ipfs.KeyChainAsync(cancel).ConfigureAwait(false);
        return await FileSystem.CreateReadStreamAsync(cid, _ipfs.Block, keyChain, cancel).ConfigureAwait(false);
    }

    public async Task<Stream> ReadFileAsync(string path, long offset, long count = 0,
        CancellationToken cancel = default)
    {
        var stream = await ReadFileAsync(path, cancel).ConfigureAwait(false);
        return new SlicedStream(stream, offset, count);
    }

    public async Task<Stream> GetAsync(string path, bool compress = false, CancellationToken cancel = default)
    {
        var cid = await _ipfs.ResolveIpfsPathToCidAsync(path, cancel).ConfigureAwait(false);
        var ms = new MemoryStream();
        await using var tarStream = new TarOutputStream(ms, 1, Encoding.UTF8);
        using var archive = TarArchive.CreateOutputTarArchive(tarStream);
        archive.IsStreamOwner = false;
        await AddTarNodeAsync(cid, cid.Encode(), tarStream, cancel).ConfigureAwait(false);

        ms.Position = 0;
        return ms;
    }

    private async Task<FileSystemNode> BuildTreeAsync(IEnumerable<FileSystemNode> nodes, AddFileOptions options, CancellationToken cancel)
    {
        while (true)
        {
            var fileSystemNodes = nodes as FileSystemNode[] ?? nodes.ToArray();
            if (fileSystemNodes.Length == 1)
            {
                return fileSystemNodes.First();
            }

            // Bundle DefaultLinksPerBlock links into a block.
            var tree = new List<FileSystemNode>();
            for (var i = 0;; ++i)
            {
                var bundle = fileSystemNodes.Skip(DefaultLinksPerBlock * i)
                    .Take(DefaultLinksPerBlock)
                    .ToArray();
                if (!bundle.Any())
                {
                    break;
                }

                var node = await BuildTreeNodeAsync(bundle, options, cancel);
                tree.Add(node);
            }

            nodes = tree;
        }
    }

    private async Task<FileSystemNode> BuildTreeNodeAsync(
        IEnumerable<FileSystemNode> nodes,
        AddFileOptions options,
        CancellationToken cancel)
    {
        var blockService = GetBlockService(options);

        // Build the DAG that contains all the file nodes.
        var fileSystemNodes = nodes as FileSystemNode[] ?? nodes.ToArray();
        var links = fileSystemNodes.Select(n => n.ToLink()).ToArray();
        var fileSize = (ulong)fileSystemNodes.Sum(n => n.Size);
        var dagSize = fileSystemNodes.Sum(n => n.DagSize);
        var dm = new DataMessage
        {
            Type = DataType.File,
            FileSize = fileSize,
            BlockSizes = fileSystemNodes.Select(n => (ulong)n.Size).ToArray()
        };
        var pb = new MemoryStream();
        Serializer.Serialize(pb, dm);
        var dag = new DagNode(pb.ToArray(), links, options.Hash);

        // Save it.
        dag.Id = await blockService.PutAsync(
            dag.ToArray(),
            multiHash: options.Hash,
            encoding: options.Encoding,
            pin: options.Pin,
            cancel: cancel).ConfigureAwait(false);

        return new()
        {
            Id = dag.Id,
            Size = (long)dm.FileSize,
            DagSize = dagSize + dag.Size,
            Links = links
        };
    }

    private async Task<FileSystemNode> CreateDirectoryAsync(IEnumerable<IFileSystemLink> links, AddFileOptions options,
        CancellationToken cancel)
    {
        var dm = new DataMessage { Type = DataType.Directory };
        var pb = new MemoryStream();
        Serializer.Serialize(pb, dm);
        var fileSystemLinks = links as IFileSystemLink[] ?? links.ToArray();
        var dag = new DagNode(pb.ToArray(), fileSystemLinks, options.Hash);

        // Save it.
        var cid = await GetBlockService(options).PutAsync(
            dag.ToArray(),
            multiHash: options.Hash,
            encoding: options.Encoding,
            pin: options.Pin,
            cancel: cancel).ConfigureAwait(false);

        return new()
        {
            Id = cid,
            Links = fileSystemLinks,
            IsDirectory = true
        };
    }

    private async Task AddTarNodeAsync(Cid cid, string name, TarOutputStream tar, CancellationToken cancel)
    {
        var block = await _ipfs.Block.GetAsync(cid, cancel).ConfigureAwait(false);
        var dm = new DataMessage { Type = DataType.Raw };
        DagNode dag = null;

        if (cid.ContentType == "dag-pb")
        {
            dag = new(block.DataStream);
            dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
        }

        var entry = new TarEntry(new TarHeader());
        var header = entry.TarHeader;
        header.Mode = 0x1ff; // 777 in octal
        header.LinkName = string.Empty;
        header.UserName = string.Empty;
        header.GroupName = string.Empty;
        header.Version = "00";
        header.Name = name;
        header.DevMajor = 0;
        header.DevMinor = 0;
        header.UserId = 0;
        header.GroupId = 0;
        header.ModTime = DateTime.Now;

        if (dm.Type == DataType.Directory)
        {
            header.TypeFlag = TarHeader.LF_DIR;
            header.Size = 0;
            await tar.PutNextEntryAsync(entry, cancel);
            await tar.CloseEntryAsync(cancel);
        }
        else // Must be a file
        {
            var content = await ReadFileAsync(cid, cancel).ConfigureAwait(false);
            header.TypeFlag = TarHeader.LF_NORMAL;
            header.Size = content.Length;
            await tar.PutNextEntryAsync(entry, cancel);
            await content.CopyToAsync(tar, cancel);
            await tar.CloseEntryAsync(cancel);
        }

        // Recurse over files and subdirectories
        if (dm.Type == DataType.Directory)
        {
            if (dag != null)
            {
                foreach (var link in dag.Links)
                {
                    await AddTarNodeAsync(link.Id, $"{name}/{link.Name}", tar, cancel).ConfigureAwait(false);
                }
            }
        }
    }


    private IBlockApi GetBlockService(AddFileOptions options)
    {
        return options.OnlyHash
            ? new HashOnlyBlockService()
            : _ipfs.Block;
    }

    /// <summary>
    ///     A Block service that only computes the block's hash.
    /// </summary>
    private class HashOnlyBlockService : IBlockApi
    {
        public Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<Cid> PutAsync(
            byte[] data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default)
        {
            var cid = new Cid
            {
                ContentType = contentType,
                Encoding = encoding,
                Hash = MultiHash.ComputeHash(data, multiHash),
                Version = contentType == "dag-pb" && multiHash == "sha2-256" ? 0 : 1
            };
            return Task.FromResult(cid);
        }

        public Task<Cid> PutAsync(
            Stream data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }
    }
}