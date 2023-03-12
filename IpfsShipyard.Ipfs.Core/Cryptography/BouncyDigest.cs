using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;

namespace IpfsShipyard.Ipfs.Core.Cryptography;

/// <summary>
///   Thin wrapper around bouncy castle digests.
/// </summary>
/// <remarks>
///   Makes a Bouncy Caslte IDigest speak .Net HashAlgorithm.
/// </remarks>
internal class BouncyDigest : HashAlgorithm
{
    private readonly IDigest _digest;

    /// <summary>
    ///   Wrap the bouncy castle digest.
    /// </summary>
    public BouncyDigest(IDigest digest)
    {
        _digest = digest;
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        _digest.Reset();
    }

    /// <inheritdoc/>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        _digest.BlockUpdate(array, ibStart, cbSize);
    }

    /// <inheritdoc/>
    protected override byte[] HashFinal()
    {
        var output = new byte[_digest.GetDigestSize()];
        _digest.DoFinal(output, 0);
        return output;
    }
}