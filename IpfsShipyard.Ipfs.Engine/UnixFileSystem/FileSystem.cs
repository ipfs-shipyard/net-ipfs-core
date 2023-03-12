using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using IpfsShipyard.Ipfs.Engine.Cryptography;
using ProtoBuf;

namespace IpfsShipyard.Ipfs.Engine.UnixFileSystem;

/// <summary>
///     Support for the *nix file system.
/// </summary>
public static class FileSystem
{
    private static readonly byte[] EmptyData = Array.Empty<byte>();

    /// <summary>
    ///     Creates a stream that can read the supplied <see cref="Cid" />.
    /// </summary>
    /// <param name="id">
    ///     The identifier of some content.
    /// </param>
    /// <param name="blockService">
    ///     The source of the cid's data.
    /// </param>
    /// <param name="keyChain">
    ///     Used to decrypt the protected data blocks.
    /// </param>
    /// <param name="cancel">
    ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task's value is
    ///     a <see cref="Stream" /> that produces the content of the <paramref name="id" />.
    /// </returns>
    /// <remarks>
    ///     The id's <see cref="Cid.ContentType" /> is used to determine how to read
    ///     the content.
    /// </remarks>
    public static Task<Stream> CreateReadStreamAsync(
        Cid id,
        IBlockApi blockService,
        KeyChain keyChain,
        CancellationToken cancel)
    {
        return id.ContentType switch
        {
            "dag-pb" => CreateDagProtoBufStreamAsync(id, blockService, keyChain, cancel),
            "raw" => CreateRawStreamAsync(id, blockService, keyChain, cancel),
            "cms" => CreateCmsStreamAsync(id, blockService, keyChain, cancel),
            _ => throw new NotSupportedException($"Cannot read content type '{id.ContentType}'.")
        };
    }

    private static async Task<Stream> CreateRawStreamAsync(
        Cid id,
        IBlockApi blockService,
        KeyChain keyChain,
        CancellationToken cancel)
    {
        var block = await blockService.GetAsync(id, cancel).ConfigureAwait(false);
        return block.DataStream;
    }

    private static async Task<Stream> CreateDagProtoBufStreamAsync(
        Cid id,
        IBlockApi blockService,
        KeyChain keyChain,
        CancellationToken cancel)
    {
        var block = await blockService.GetAsync(id, cancel).ConfigureAwait(false);
        var dag = new DagNode(block.DataStream);
        var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);

        if (dm.Type != DataType.File)
        {
            throw new($"'{id.Encode()}' is not a file.");
        }

        if (dm.Fanout.HasValue)
        {
            throw new NotImplementedException("files with a fanout");
        }

        // Is it a simple node?
        if (dm.BlockSizes == null && !dm.Fanout.HasValue)
        {
            return new MemoryStream(dm.Data ?? EmptyData, false);
        }

        if (dm.BlockSizes != null)
        {
            return new ChunkedStream(blockService, keyChain, dag);
        }

        throw new($"Cannot determine the file format of '{id}'.");
    }

    private static async Task<Stream> CreateCmsStreamAsync(
        Cid id,
        IBlockApi blockService,
        KeyChain keyChain,
        CancellationToken cancel)
    {
        var block = await blockService.GetAsync(id, cancel).ConfigureAwait(false);
        var plain = await keyChain.ReadProtectedDataAsync(block.DataBytes, cancel).ConfigureAwait(false);
        return new MemoryStream(plain, false);
    }
}