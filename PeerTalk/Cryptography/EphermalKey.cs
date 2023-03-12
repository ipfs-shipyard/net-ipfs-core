using System.Collections.Generic;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace IpfsShipyard.PeerTalk.Cryptography;

/// <summary>
///   A short term key on a curve.
/// </summary>
/// <remarks>
///   Ephemeral keys are different from other keys in IPFS; they are NOT
///   protobuf encoded and are NOT self describing.  The encoding is an
///   uncompressed ECPoint; the first byte s a 4 and followed by X and Y co-ordinates.
///   <para>
///   It as assumed that the curve name is known a-priori.
///   </para>
/// </remarks>
public class EphermalKey
{
    private ECPublicKeyParameters _publicKey;
    private ECPrivateKeyParameters _privateKey;

    /// <summary>
    ///   Gets the IPFS encoding of the public key.
    /// </summary>
    /// <returns>
    ///   Returns the uncompressed EC point.
    /// </returns>
    public byte[] PublicKeyBytes()
    {
        return _publicKey.Q.GetEncoded(compressed: false);
    }

    /// <summary>
    ///   Create a shared secret between this key and another.
    /// </summary>
    /// <param name="other">
    ///   Another ephemeral key.
    /// </param>
    /// <returns>
    ///   The shared secret as a byte array.
    /// </returns>
    /// <remarks>
    ///   Uses the ECDH agreement algorithm to generate the shared secret.
    /// </remarks>
    public byte[] GenerateSharedSecret(EphermalKey other)
    {
        var agreement = AgreementUtilities.GetBasicAgreement("ECDH");
        agreement.Init(_privateKey);
        var secret = agreement.CalculateAgreement(other._publicKey);
        return BigIntegers.AsUnsignedByteArray(agreement.GetFieldSize(), secret);
    }

    /// <summary>
    ///   Create a public key from the IPFS ephemeral encoding.
    /// </summary>
    /// <param name="curveName">
    ///   The name of the curve, for example "P-256".
    /// </param>
    /// <param name="bytes">
    ///   The IPFS encoded ephemeral key.
    /// </param>
    public static EphermalKey CreatePublicKeyFromIpfs(string curveName, byte[] bytes)
    {
        var ecP = ECNamedCurveTable.GetByName(curveName);
        if (ecP == null)
            throw new KeyNotFoundException($"Unknown curve name '{curveName}'.");
        var domain = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());
        var q = ecP.Curve.DecodePoint(bytes);
        return new()
        {
            _publicKey = new(q, domain)
        };
    }

    /// <summary>
    ///   Create a new ephemeral key on the curve.
    /// </summary>
    /// <param name="curveName">
    ///   The name of the curve, for example "P-256".
    /// </param>
    /// <returns>
    ///   The new created ephemeral key.
    /// </returns>
    public static EphermalKey Generate(string curveName)
    {
        var ecP = ECNamedCurveTable.GetByName(curveName);
        if (ecP == null)
            throw new($"Unknown curve name '{curveName}'.");
        var domain = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());
        var g = GeneratorUtilities.GetKeyPairGenerator("EC");
        g.Init(new ECKeyGenerationParameters(domain, new()));
        var keyPair = g.GenerateKeyPair();

        return new()
        {
            _privateKey = (ECPrivateKeyParameters)keyPair.Private,
            _publicKey = (ECPublicKeyParameters)keyPair.Public
        };
    }
}