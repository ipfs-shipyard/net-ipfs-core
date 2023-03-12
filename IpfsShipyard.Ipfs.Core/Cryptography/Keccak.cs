/*
 * The package was named SHA3 it is really Keccak
 * https://medium.com/@ConsenSys/are-you-really-using-sha-3-or-old-code-c5df31ad2b0
 * See 
 * 
 * The SHA3 package doesn't create .Net Standard package.
 * This is a copy of https://bitbucket.org/jdluzen/sha3/raw/d1fd55dc225d18a7fb61515b62d3c8f164d2e788/SHA3/SHA3.cs
 */

using System;

namespace IpfsShipyard.Ipfs.Core.Cryptography;

internal abstract class Keccak : System.Security.Cryptography.HashAlgorithm
{

    #region Implementation
    public const int KeccakB = 1600;
    public const int KeccakNumberOfRounds = 24;
    public const int KeccakLaneSizeInBits = 8 * 8;

    public readonly ulong[] RoundConstants;

    protected ulong[] state;
    protected byte[] Buffer;
    protected int BuffLength;
    protected int keccakR;

    public int KeccakR
    {
        get => keccakR;
        protected set => keccakR = value;
    }

    public int SizeInBytes => KeccakR / 8;

    public int HashByteLength => HashSizeValue / 8;

    public override bool CanReuseTransform => true;

    protected Keccak(int hashBitLength)
    {
        if (hashBitLength != 224 && hashBitLength != 256 && hashBitLength != 384 && hashBitLength != 512)
            throw new ArgumentException("hashBitLength must be 224, 256, 384, or 512", "hashBitLength");
        Initialize();
        HashSizeValue = hashBitLength;
        switch (hashBitLength)
        {
            case 224:
                KeccakR = 1152;
                break;
            case 256:
                KeccakR = 1088;
                break;
            case 384:
                KeccakR = 832;
                break;
            case 512:
                KeccakR = 576;
                break;
        }
        RoundConstants = new ulong[]
        {
            0x0000000000000001UL,
            0x0000000000008082UL,
            0x800000000000808aUL,
            0x8000000080008000UL,
            0x000000000000808bUL,
            0x0000000080000001UL,
            0x8000000080008081UL,
            0x8000000000008009UL,
            0x000000000000008aUL,
            0x0000000000000088UL,
            0x0000000080008009UL,
            0x000000008000000aUL,
            0x000000008000808bUL,
            0x800000000000008bUL,
            0x8000000000008089UL,
            0x8000000000008003UL,
            0x8000000000008002UL,
            0x8000000000000080UL,
            0x000000000000800aUL,
            0x800000008000000aUL,
            0x8000000080008081UL,
            0x8000000000008080UL,
            0x0000000080000001UL,
            0x8000000080008008UL
        };
    }

    protected ulong Rol(ulong a, int offset)
    {
        return (((a) << ((offset) % KeccakLaneSizeInBits)) ^ ((a) >> (KeccakLaneSizeInBits - ((offset) % KeccakLaneSizeInBits))));
    }

    protected void AddToBuffer(byte[] array, ref int offset, ref int count)
    {
        var amount = Math.Min(count, Buffer.Length - BuffLength);
        System.Buffer.BlockCopy(array, offset, Buffer, BuffLength, amount);
        offset += amount;
        BuffLength += amount;
        count -= amount;
    }

    public override byte[] Hash => HashValue;

    public override int HashSize => HashSizeValue;

    #endregion

    public override void Initialize()
    {
        BuffLength = 0;
        state = new ulong[5 * 5];//1600 bits
        HashValue = null;
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        if (array == null)
            throw new ArgumentNullException("array");
        if (ibStart < 0)
            throw new ArgumentOutOfRangeException("ibStart");
        if (cbSize > array.Length)
            throw new ArgumentOutOfRangeException("cbSize");
        if (ibStart + cbSize > array.Length)
            throw new ArgumentOutOfRangeException("ibStart or cbSize");
    }
}