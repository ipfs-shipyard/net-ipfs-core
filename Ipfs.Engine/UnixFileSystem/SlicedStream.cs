using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine.UnixFileSystem;

/// <summary>
///     Provides read only access to a slice of stream.
/// </summary>
internal class SlicedStream : Stream
{
    private readonly long _logicalEnd;
    private readonly long _offset;
    private readonly Stream _stream;

    public SlicedStream(Stream stream, long offset, long count)
    {
        _stream = stream;
        _offset = offset;

        stream.Position = offset;
        _logicalEnd = count < 1
            ? stream.Length
            : Math.Min(stream.Length, offset + count);
    }

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _stream.Length;

    public override bool CanTimeout => _stream.CanTimeout;

    public override int ReadTimeout
    {
        get => _stream.ReadTimeout;
        set => _stream.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => _stream.WriteTimeout;
        set => _stream.WriteTimeout = value;
    }

    public override long Position
    {
        get => _stream.Position - _offset;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        _stream.Flush();
    }

    public override int ReadByte()
    {
        if (_stream.Position >= _logicalEnd)
        {
            return -1;
        }

        return _stream.ReadByte();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_stream.Position >= _logicalEnd)
        {
            return 0;
        }

        var length = Math.Min(count, _logicalEnd - _stream.Position);
        var n = _stream.Read(buffer, offset, (int)length);
        return n;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _stream.FlushAsync(cancellationToken);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override void WriteByte(byte value)
    {
        throw new NotSupportedException();
    }
}