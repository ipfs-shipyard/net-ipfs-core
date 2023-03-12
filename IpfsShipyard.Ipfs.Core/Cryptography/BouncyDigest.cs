namespace IpfsShipyard.Ipfs.Core.Cryptography;

/// <summary>
///   Thin wrapper around bouncy castle digests.
/// </summary>
/// <remarks>
///   Makes a Bouncy Caslte IDigest speak .Net HashAlgorithm.
/// </remarks>
internal class BouncyDigest : System.Security.Cryptography.HashAlgorithm
{
    private readonly Org.BouncyCastle.Crypto.IDigest _digest;

    /// <summary>
    ///   Wrap the bouncy castle digest.
    /// </summary>
    public BouncyDigest(Org.BouncyCastle.Crypto.IDigest digest)
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