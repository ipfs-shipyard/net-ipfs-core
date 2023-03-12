using System;
using System.Security.Cryptography;

namespace IpfsShipyard.Ipfs.Core.Cryptography;

internal class IdentityHash : HashAlgorithm
{
    private byte[] _digest;

    public override void Initialize()
    {
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        if (_digest == null)
        {
            _digest = new byte[cbSize];
            Buffer.BlockCopy(array, ibStart, _digest, 0, cbSize);
            return;
        }

        var buffer = new byte[_digest.Length + cbSize];
        Buffer.BlockCopy(_digest, 0, buffer, _digest.Length, _digest.Length);
        Buffer.BlockCopy(array, ibStart, _digest, _digest.Length, cbSize);
        _digest = buffer;
    }

    protected override byte[] HashFinal()
    {
        return _digest;
    }
}