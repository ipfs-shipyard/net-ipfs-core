using System;
using System.Security.Cryptography;

namespace IpfsShipyard.Ipfs.Core.Cryptography;

internal class DoubleSha256 : HashAlgorithm
{
    private readonly HashAlgorithm _digest = SHA256.Create();
    private byte[] _round1;

    public override void Initialize()
    {
        _digest.Initialize();
        _round1 = null;
    }

    public override int HashSize => _digest.HashSize;

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        if (_round1 != null)
            throw new NotSupportedException("Already called.");

        _round1 = _digest.ComputeHash(array, ibStart, cbSize);
    }

    protected override byte[] HashFinal()
    {
        _digest.Initialize();
        return _digest.ComputeHash(_round1);
    }

}