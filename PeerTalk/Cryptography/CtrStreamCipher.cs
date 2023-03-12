using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities;

namespace IpfsShipyard.PeerTalk.Cryptography;

/// <summary>
///   The CTR cipher as a stream.
/// </summary>
/// <remarks>
///   A copy of <see href="https://github.com/onovotny/bc-csharp/blob/netstandard/crypto/src/crypto/modes/SicBlockCipher.cs">SicBlockCipher</see>
///   that implements <see cref="IStreamCipher"/>.
/// </remarks>
public class CtrStreamCipher : IStreamCipher
{
    private readonly IBlockCipher _cipher;
    private readonly int _blockSize;
    private readonly byte[] _counter;
    private readonly byte[] _counterOut;
    private int _byteCount;
    private byte[] _iv;

    /// <summary>
    ///   Creates a new instance of the <see cref="CtrStreamCipher"/> with
    ///   the specified cipher.
    /// </summary>
    /// <param name="cipher">
    ///   The cipher to produce the output counter.  Typically
    ///   <see cref="Org.BouncyCastle.Crypto.Engines.AesEngine"/>.
    /// </param>
    public CtrStreamCipher(IBlockCipher cipher)
    {
        _cipher = cipher;
        _blockSize = cipher.GetBlockSize();
        _counter = new byte[_blockSize];
        _counterOut = new byte[_blockSize];
        _iv = new byte[_blockSize];
    }

    /// <summary>
    ///   The name of this algorithm.
    /// </summary>
    public string AlgorithmName => _cipher.AlgorithmName + "/CTR";

    /// <summary>
    ///   Init the cipher.
    /// </summary>
    /// <param name="forEncryption">
    ///   Ignored.
    /// </param>
    /// <param name="parameters">
    ///   Must be a <see cref="ParametersWithIV"/>.
    /// </param>
    /// <example>
    /// var encrypt = new CtrStreamCipher(new AesEngine());
    /// var p = new ParametersWithIV(new KeyParameter(key), iv);
    /// encrypt.Init(true, p);
    /// </example>
    public void Init(bool forEncryption, ICipherParameters parameters)
    {
        var ivParam = parameters as ParametersWithIV;
        if (ivParam == null)
            throw new ArgumentException("CTR mode requires ParametersWithIV", nameof(parameters));

        _iv = Arrays.Clone(ivParam.GetIV());

        if (_blockSize < _iv.Length)
            throw new ArgumentException("CTR mode requires IV no greater than: " + _blockSize + " bytes.");

        var maxCounterSize = Math.Min(8, _blockSize / 2);
        if (_blockSize - _iv.Length > maxCounterSize)
            throw new ArgumentException("CTR mode requires IV of at least: " + (_blockSize - maxCounterSize) + " bytes.");

        // if null it's an IV changed only.
        if (ivParam.Parameters != null)
        {
            _cipher.Init(true, ivParam.Parameters);
        }

        Reset();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _byteCount = 0;
        Arrays.Fill(_counter, (byte)0);
        Array.Copy(_iv, 0, _counter, 0, _iv.Length);
        _cipher.Reset();
    }

    /// <inheritdoc />
    public void ProcessBytes(byte[] input, int inOff, int length, byte[] output, int outOff)
    {
        if (outOff + length > output.Length)
        {
            throw new DataLengthException("Output buffer too short");
        }

        if (inOff + length > input.Length)
        {
            throw new DataLengthException("Input buffer too small");
        }

        var inStart = inOff;
        var inEnd = inOff + length;
        var outStart = outOff;

        while (inStart < inEnd)
        {
            output[outStart++] = ReturnByte(input[inStart++]);
        }
    }

    /// <inheritdoc />
    public byte ReturnByte(byte input)
    {
        if (_byteCount == 0)
        {
            _cipher.ProcessBlock(_counter, 0, _counterOut, 0);
            // Increment the counter
            var j = _counter.Length;
            while (--j >= 0 && ++_counter[j] == 0)
            {
            }
        }

        var rv = (byte)(_counterOut[_byteCount++] ^ input);
        if (_byteCount == _counterOut.Length)
        {
            _byteCount = 0;
        }
        return rv;
    }
}