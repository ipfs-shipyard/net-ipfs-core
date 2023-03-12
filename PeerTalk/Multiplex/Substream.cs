using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.PeerTalk.Multiplex;

/// <summary>
///   A duplex substream used by the <see cref="Muxer"/>.
/// </summary>
/// <remarks>
///   Reading of data waits on the Muxer calling <see cref="AddData(byte[])"/>.
///   <see cref="NoMoreData"/> is used to signal the end of stream.
///   <para>
///   Writing data is buffered until <see cref="FlushAsync(CancellationToken)"/> is
///   called.
///   </para>
/// </remarks>
public class Substream : Stream
{
    private readonly BufferBlock<byte[]> _inBlocks = new();
    private byte[] _inBlock;
    private int _inBlockOffset;
    private bool _eos;

    private Stream _outStream = new MemoryStream();

    /// <summary>
    ///   The type of message of sent to the other side.
    /// </summary>
    /// <value>
    ///   Either <see cref="PacketType.MessageInitiator"/> or <see cref="PacketType.MessageReceiver"/>.
    ///   Defaults to <see cref="PacketType.MessageReceiver"/>.
    /// </value>
    public PacketType SentMessageType = PacketType.MessageReceiver;

    /// <summary>
    ///   The stream identifier.
    /// </summary>
    /// <value>
    ///   The session initiator allocates odd IDs and the session receiver allocates even IDs.
    /// </value>
    public long Id;

    /// <summary>
    ///   A name for the stream.
    /// </summary>
    /// <value>
    ///   Names do not need to be unique.
    /// </value>
    public string Name;

    /// <summary>
    ///   The multiplexor associated with the substream.
    /// </summary>
    public Muxer Muxer { get; set; }

    /// <inheritdoc />
    public override bool CanRead => !_eos;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => _outStream != null;

    /// <inheritdoc />
    public override bool CanTimeout => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///   Add some data that should be read by the stream.
    /// </summary>
    /// <param name="data">
    ///   The data to be read.
    /// </param>
    /// <remarks>
    ///   <b>AddData</b> is called when the muxer receives a packet for this
    ///   stream.
    /// </remarks>
    public void AddData(byte[] data)
    {
        _inBlocks.Post(data);
    }

    /// <summary>
    ///   Indicates that the stream will not receive any more data.
    /// </summary>
    /// <seealso cref="AddData(byte[])"/>
    /// <remarks>
    ///   <b>NoMoreData</b> is called when the muxer receives a packet to
    ///   close this stream.
    /// </remarks>
    public void NoMoreData()
    {
        _inBlocks.Complete();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
#pragma warning disable VSTHRD002
        return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var total = 0;
        while (count > 0 && !_eos)
        {
            // Does the current block have some unread data?
            if (_inBlock != null && _inBlockOffset < _inBlock.Length)
            {
                var n = Math.Min(_inBlock.Length - _inBlockOffset, count);
                Array.Copy(_inBlock, _inBlockOffset, buffer, offset, n);
                total += n;
                count -= n;
                offset += n;
                _inBlockOffset += n;
            }
            // Otherwise, wait for a new block of data.
            else
            {
                try
                {
                    _inBlock = await _inBlocks.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                    _inBlockOffset = 0;
                }
                catch (InvalidOperationException) // no more data!
                {
                    _eos = true;
                }
            }
        }
        return total;
    }

    /// <inheritdoc />
    public override void Flush()
    {
#pragma warning disable VSTHRD002
        FlushAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <inheritdoc />
    public override async Task FlushAsync(CancellationToken cancel)
    {
        if (_outStream.Length == 0)
            return;

        // Send the response over the muxer channel
        using (await Muxer.AcquireWriteAccessAsync().ConfigureAwait(false))
        {
            _outStream.Position = 0;
            var header = new Header
            {
                StreamId = Id,
                PacketType = SentMessageType
            };
            await header.WriteAsync(Muxer.Channel, cancel).ConfigureAwait(false);
            await Muxer.Channel.WriteVarintAsync(_outStream.Length, cancel).ConfigureAwait(false);
            await _outStream.CopyToAsync(Muxer.Channel).ConfigureAwait(false);
            await Muxer.Channel.FlushAsync(cancel).ConfigureAwait(false);

            _outStream.SetLength(0);
        }
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        _outStream.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _outStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override void WriteByte(byte value)
    {
        _outStream.WriteByte(value);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO: find a better method
            _ = Task.Run(async () => await Muxer?.RemoveStreamAsync(this));

            _eos = true;
            if (_outStream != null)
            {
                _outStream.Dispose();
                _outStream = null;
            }
        }
        base.Dispose(disposing);
    }
}