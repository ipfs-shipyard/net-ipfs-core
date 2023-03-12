using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using IpfsShipyard.Ipfs.Engine.Cryptography;
using ProtoBuf;

namespace IpfsShipyard.Ipfs.Engine.UnixFileSystem;

/// <summary>
///     Provides read-only access to a chunked file.
/// </summary>
/// <remarks>
///     Internal class to support <see cref="FileSystem" />.
/// </remarks>
public class ChunkedStream : Stream
{
    private readonly List<BlockInfo> _blocks = new();

    private BlockInfo _currentBlock;
    private byte[] _currentData;

    /// <summary>
    ///     Creates a new instance of the <see cref="ChunkedStream" /> class with
    ///     the specified <see cref="IBlockApi" /> and <see cref="DagNode" />.
    /// </summary>
    /// <param name="blockService"></param>
    /// <param name="keyChain"></param>
    /// <param name="dag"></param>
    public ChunkedStream(IBlockApi blockService, KeyChain keyChain, DagNode dag)
    {
        BlockService = blockService;
        KeyChain = keyChain;
        var links = dag.Links.ToArray();
        var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
        if (dm.FileSize != null)
        {
            Length = (long)dm.FileSize;
        }

        ulong position = 0;
        for (var i = 0; i < dm.BlockSizes.Length; ++i)
        {
            _blocks.Add(new()
            {
                Id = links[i].Id,
                Position = (long)position
            });
            position += dm.BlockSizes[i];
        }
    }

    private IBlockApi BlockService { get; }
    private KeyChain KeyChain { get; }

    /// <inheritdoc />
    public override long Length { get; }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Position { get; set; }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            case SeekOrigin.End:
                Position = Length - offset;
                break;
        }

        return Position;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancel)
    {
        var block = await GetBlockAsync(Position, cancel).ConfigureAwait(false);
        var k = Math.Min(count, block.Count);
        if (k <= 0)
        {
            return k;
        }

        ArgumentNullException.ThrowIfNull(block.Array);
        Array.Copy(block.Array, block.Offset, buffer, offset, k);
        Position += k;

        return k;
    }

    private async Task<ArraySegment<byte>> GetBlockAsync(long position, CancellationToken cancel)
    {
        if (position >= Length)
        {
            return new();
        }

        var need = _blocks.Last(b => b.Position <= position);
        if (need != _currentBlock)
        {
            var stream = await FileSystem.CreateReadStreamAsync(need.Id, BlockService, KeyChain, cancel)
                .ConfigureAwait(false);
            _currentBlock = need;
            _currentData = new byte[stream.Length];
            for (int i = 0, n; i < stream.Length; i += n)
            {
                n = await stream.ReadAsync(_currentData, i, (int)stream.Length - i, cancel);
            }
        }

        var offset = (int)(position - _currentBlock.Position);
        return new(_currentData, offset, _currentData.Length - offset);
    }

    private class BlockInfo
    {
        public Cid Id;
        public long Position;
    }
}