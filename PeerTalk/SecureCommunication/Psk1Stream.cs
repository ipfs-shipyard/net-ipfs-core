using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.PeerTalk.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace IpfsShipyard.PeerTalk.SecureCommunication;

/// <summary>
///   A duplex stream that is encrypted with a <see cref="PreSharedKey"/>.
/// </summary>
/// <remarks>
///   The XSalsa20 cipher is used to encrypt the data.
/// </remarks>
/// <seealso href="https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md"/>
public class Psk1Stream : Stream
{
    private const int KeyBitLength = 256;
    private const int NonceBitLength = 192;
    private const int NonceByteLength = NonceBitLength / 8;

    private readonly Stream _stream;
    private readonly PreSharedKey _key;
    private IStreamCipher _readCipher;
    private IStreamCipher _writeCipher;

    /// <summary>
    ///   Creates a new instance of the <see cref="Psk1Stream"/> class.
    /// </summary>
    /// <param name="stream">
    ///   The source/destination of the unprotected stream.
    /// </param>
    /// <param name="key">
    ///   The pre-shared 256-bit key for the private network of peers.
    /// </param>
    public Psk1Stream(
        Stream stream,
        PreSharedKey key)
    {
        if (key.Length != KeyBitLength)
            throw new($"The pre-shared key must be {KeyBitLength} bits in length.");

        _stream = stream;
        _key = key;
    }

    private IStreamCipher WriteCipher
    {
        get
        {
            if (_writeCipher == null)
            {
                // Get a random nonce
                var nonce = new byte[NonceByteLength];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nonce);
                }

                // Send the nonce to the remote
                _stream.Write(nonce, 0, nonce.Length);

                // Create the cipher
                _writeCipher = new XSalsa20Engine();
                _writeCipher.Init(true, new ParametersWithIV(new KeyParameter(_key.Value), nonce));
            }
            return _writeCipher;
        }
    }

    private IStreamCipher ReadCipher
    {
        get
        {
            if (_readCipher == null)
            {
                // Get the nonce from the remote.
                var nonce = new byte[NonceByteLength];
                for (int i = 0, n; i < NonceByteLength; i += n)
                {
                    n = _stream.Read(nonce, i, NonceByteLength - i);
                    if (n < 1)
                        throw new EndOfStreamException();
                }

                // Create the cipher
                _readCipher = new XSalsa20Engine();
                _readCipher.Init(false, new ParametersWithIV(new KeyParameter(_key.Value), nonce));
            }
            return _readCipher;
        }
    }

    /// <inheritdoc />
    public override bool CanRead => _stream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => _stream.CanWrite;

    /// <inheritdoc />
    public override bool CanTimeout => _stream.CanTimeout;

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

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        var cipher = ReadCipher;
        var n = _stream.Read(buffer, offset, count);
        cipher.ProcessBytes(buffer, offset, n, buffer, offset);
        return n;
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var cipher = ReadCipher;
        var n = await _stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        cipher.ProcessBytes(buffer, offset, n, buffer, offset);
        return n;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        _stream.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancel)
    {
        return _stream.FlushAsync(cancel);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        var x = new byte[count];
        WriteCipher.ProcessBytes(buffer, offset, count, x, 0);
        _stream.Write(x, 0, count);
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var x = new byte[count];
        WriteCipher.ProcessBytes(buffer, offset, count, x, 0);
        return _stream.WriteAsync(x, 0, count, cancellationToken);
    }

    /// <inheritdoc />
    public override void WriteByte(byte value)
    {
        _stream.WriteByte(WriteCipher.ReturnByte(value));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream.Dispose();
        }
        base.Dispose(disposing);
    }
}