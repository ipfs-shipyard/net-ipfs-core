using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;

namespace IpfsShipyard.Ipfs.Engine.CoreApi;

[DataContract]
internal class DataBlock : IDataBlock
{
    [DataMember] public byte[] DataBytes { get; set; }

    public Stream DataStream => new MemoryStream(DataBytes, false);

    [DataMember] public Cid Id { get; set; }

    [DataMember] public long Size { get; set; }
}

internal class BlockApi : IBlockApi
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(BlockApi));

    private static readonly DataBlock EmptyDirectory = new()
    {
        DataBytes = ObjectApi.EmptyDirectory.ToArray(),
        Id = ObjectApi.EmptyDirectory.Id,
        Size = ObjectApi.EmptyDirectory.ToArray().Length
    };

    private static readonly DataBlock EmptyNode = new()
    {
        DataBytes = ObjectApi.EmptyNode.ToArray(),
        Id = ObjectApi.EmptyNode.Id,
        Size = ObjectApi.EmptyNode.ToArray().Length
    };

    private readonly IpfsEngine _ipfs;
    private FileStore<Cid, DataBlock> _store;

    public BlockApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public FileStore<Cid, DataBlock> Store
    {
        get
        {
            if (_store != null)
            {
                return _store;
            }

            var folder = Path.Combine(_ipfs.Options.Repository.Folder, "blocks");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _store = new()
            {
                Folder = folder,
                NameToKey = cid => cid.Hash.ToBase32(),
                KeyToName = key => new MultiHash(key.FromBase32()),
                Serialize = async (stream, cid, block, cancel) =>
                {
                    await stream.WriteAsync(block.DataBytes, 0, block.DataBytes.Length, cancel)
                        .ConfigureAwait(false);
                },
                Deserialize = async (stream, cid, cancel) =>
                {
                    var block = new DataBlock
                    {
                        Id = cid,
                        Size = stream.Length
                    };
                    block.DataBytes = new byte[block.Size];
                    for (int i = 0, n; i < block.Size; i += n)
                    {
                        n = await stream.ReadAsync(block.DataBytes, i, (int)block.Size - i, cancel)
                            .ConfigureAwait(false);
                    }

                    return block;
                }
            };

            return _store;
        }
    }


    public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default)
    {
        // Hack for empty object and empty directory object
        if (id == EmptyDirectory.Id)
        {
            return EmptyDirectory;
        }

        if (id == EmptyNode.Id)
        {
            return EmptyNode;
        }

        // If identity hash, then CID has the content.
        if (id.Hash.IsIdentityHash)
        {
            return new DataBlock
            {
                DataBytes = id.Hash.Digest,
                Id = id,
                Size = id.Hash.Digest.Length
            };
        }

        // Check the local filesystem for the block.
        var block = await Store.TryGetAsync(id, cancel).ConfigureAwait(false);
        if (block != null)
        {
            return block;
        }

        // Query the network, via DHT, for peers that can provide the
        // content.  As a provider peer is found, it is connected to and
        // the bitswap want lists are exchanged.  Hopefully the provider will
        // then send the block to us via bitswap and the get task will finish.
        using var queryCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel);

        var queryCancelToken = queryCancel.Token;
        var bitswapGet = _ipfs.Bitswap.GetAsync(id, queryCancelToken).ConfigureAwait(false);
        var dht = await _ipfs.DhtService;
        var _ = dht.FindProvidersAsync(
            id, // TODO: remove this
            cancel: queryCancelToken,
            action: peer =>
            {
                var __ = ProviderFoundAsync(peer, queryCancelToken).ConfigureAwait(false);
            }
        );

        var got = await bitswapGet;
        Log.Debug("bitswap got the block");

        queryCancel.Cancel(false); // stop the network query.
        return got;
    }

    public async Task<Cid> PutAsync(
        byte[] data,
        string contentType = Cid.DefaultContentType,
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = false,
        CancellationToken cancel = default)
    {
        if (data.Length > _ipfs.Options.Block.MaxBlockSize)
        {
            throw new ArgumentOutOfRangeException(nameof(data.Length),
                $"Block length can not exceed {_ipfs.Options.Block.MaxBlockSize}.");
        }

        // Small enough for an inline CID?
        if (_ipfs.Options.Block.AllowInlineCid && data.Length <= _ipfs.Options.Block.InlineCidLimit)
        {
            return new()
            {
                ContentType = contentType,
                Hash = MultiHash.ComputeHash(data, "identity")
            };
        }

        // CID V1 encoding defaulting to base32 which is not
        // the multibase default. 
        var cid = new Cid
        {
            ContentType = contentType,
            Hash = MultiHash.ComputeHash(data, multiHash)
        };
        if (encoding != "base58btc")
        {
            cid.Encoding = encoding;
        }

        var block = new DataBlock
        {
            DataBytes = data,
            Id = cid,
            Size = data.Length
        };
        if (await Store.ExistsAsync(cid, cancel).ConfigureAwait(false))
        {
            Log.DebugFormat("Block '{0}' already present", cid);
        }
        else
        {
            await Store.PutAsync(cid, block, cancel).ConfigureAwait(false);
            if (_ipfs.IsStarted)
            {
                await _ipfs.Dht.ProvideAsync(cid, false, cancel).ConfigureAwait(false);
            }

            Log.DebugFormat("Added block '{0}'", cid);
        }

        // Inform the Bitswap service.
        (await _ipfs.BitswapService.ConfigureAwait(false)).Found(block);

        // To pin or not.
        if (pin)
        {
            await _ipfs.Pin.AddAsync(cid, false, cancel).ConfigureAwait(false);
        }
        else
        {
            await _ipfs.Pin.RemoveAsync(cid, false, cancel).ConfigureAwait(false);
        }

        return cid;
    }

    public async Task<Cid> PutAsync(
        Stream data,
        string contentType = Cid.DefaultContentType,
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = false,
        CancellationToken cancel = default)
    {
        using var ms = new MemoryStream();
        await data.CopyToAsync(ms, cancel).ConfigureAwait(false);
        return await PutAsync(ms.ToArray(), contentType, multiHash, encoding, pin, cancel).ConfigureAwait(false);
    }

    public async Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default)
    {
        if (id.Hash.IsIdentityHash)
        {
            return id;
        }

        if (!await Store.ExistsAsync(id, cancel).ConfigureAwait(false))
        {
            return ignoreNonexistent ? null : throw new KeyNotFoundException($"Block '{id.Encode()}' does not exist.");
        }

        await Store.RemoveAsync(id, cancel).ConfigureAwait(false);
        await _ipfs.Pin.RemoveAsync(id, false, cancel).ConfigureAwait(false);
        return id;

    }

    public async Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default)
    {
        if (id.Hash.IsIdentityHash)
        {
            return await GetAsync(id, cancel).ConfigureAwait(false);
        }

        IDataBlock block = null;
        var length = await Store.LengthAsync(id, cancel).ConfigureAwait(false);
        if (length.HasValue)
        {
            block = new DataBlock
            {
                Id = id,
                Size = length.Value
            };
        }

        return block;
    }

    private async Task ProviderFoundAsync(Peer peer, CancellationToken cancel)
    {
        if (cancel.IsCancellationRequested)
        {
            return;
        }

        Log.Debug($"Connecting to provider {peer.Id}");
        var swarm = await _ipfs.SwarmService.ConfigureAwait(false);
        try
        {
            await swarm.ConnectAsync(peer, cancel).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Warn($"Connection to provider {peer.Id} failed, {e.Message}");
        }
    }
}