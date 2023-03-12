// BufferedStream is not available in Net Stardard 1.4
#if !NETSTANDARD14

// Part of JuiceStream: https://juicestream.machinezoo.com
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IpfsShipyard.PeerTalk
{
    /// <summary>
    /// .NET already has its <c>BufferedStream</c>, but that one will throw unexpected exceptions, especially on <c>NetworkStreams</c>.
    /// JuiceStream's <c>DuplexBufferedStream</c> embeds two <c>BufferedStream</c> instances,
    /// one for each direction, to provide full duplex buffering over non-seekable streams.
    /// </summary>
    /// <remarks>
    ///   Copied from <see href="https://bitbucket.org/robertvazan/juicestream/raw/2caa975524900d1b5a76ddd3731c273d5dbb51eb/JuiceStream/DuplexBufferedStream.cs"/>
    /// </remarks>
    internal class DuplexBufferedStream : Stream
    {
        private readonly Stream _inner;
        private readonly BufferedStream _readBuffer;
        private readonly BufferedStream _writeBuffer;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public DuplexBufferedStream(Stream stream)
        {
            _inner = stream;
            _readBuffer = new(stream);
            _writeBuffer = new(stream);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writeBuffer.Flush();
                _inner.Dispose();
                _readBuffer.Dispose();
                _writeBuffer.Dispose();
            }
        }

        public override void Flush() { _writeBuffer.Flush(); }
        public override Task FlushAsync(CancellationToken token) { return _writeBuffer.FlushAsync(token); }
        public override int Read(byte[] buffer, int offset, int count) { return _readBuffer.Read(buffer, offset, count); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) { return _readBuffer.ReadAsync(buffer, offset, count, token); }
        public override int ReadByte() { return _readBuffer.ReadByte(); }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { _writeBuffer.Write(buffer, offset, count); }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) { return _writeBuffer.WriteAsync(buffer, offset, count, token); }
        public override void WriteByte(byte value) { _writeBuffer.WriteByte(value); }
    }
}

#endif