using System;
using System.Security.Cryptography;

namespace IpfsShipyard.Ipfs.Core.Cryptography;

class DoubleSha256 : HashAlgorithm
{
    HashAlgorithm _digest = SHA256.Create();
    byte[] _round1;

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