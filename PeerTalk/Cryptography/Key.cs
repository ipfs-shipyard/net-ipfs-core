using System;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using ProtoBuf;

namespace IpfsShipyard.PeerTalk.Cryptography;

/// <summary>
///   An asymmetric key.
/// </summary>
public sealed class Key
{
    private const string RsaSigningAlgorithmName = "SHA-256withRSA";
    private const string EcSigningAlgorithmName = "SHA-256withECDSA";
    private const string Ed25519SigningAlgorithmName = "Ed25519";

    private AsymmetricKeyParameter _publicKey;
    private AsymmetricKeyParameter _privateKey;
    private string _signingAlgorithmName;

    private Key()
    {
    }

    /// <summary>
    ///   Verify that signature matches the data.
    /// </summary>
    /// <param name="data">
    ///   The data to check.
    /// </param>
    /// <param name="signature">
    ///   The supplied signature of the <paramref name="data"/>.
    /// </param>
    /// <exception cref="InvalidDataException">
    ///   The <paramref name="data"/> does match the <paramref name="signature"/>.
    /// </exception>
    public void Verify(byte[] data, byte[] signature)
    {
        var signer = SignerUtilities.GetSigner(_signingAlgorithmName);
        signer.Init(false, _publicKey);
        signer.BlockUpdate(data, 0, data.Length);
        if (!signer.VerifySignature(signature))
            throw new InvalidDataException("Data does not match the signature.");
    }

    /// <summary>
    ///   Create a signature for the data.
    /// </summary>
    /// <param name="data">
    ///   The data to sign.
    /// </param>
    /// <returns>
    ///   The signature.
    /// </returns>
    public byte[] Sign(byte[] data)
    {
        var signer = SignerUtilities.GetSigner(_signingAlgorithmName);
        signer.Init(true, _privateKey);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    /// <summary>
    ///   Create a public key from the IPFS message.
    /// </summary>
    /// <param name="bytes">
    ///   The IPFS encoded protobuf PublicKey message.
    /// </param>
    /// <returns>
    ///   The public key.
    /// </returns>
    public static Key CreatePublicKeyFromIpfs(byte[] bytes)
    {
        var key = new Key();

        var ms = new MemoryStream(bytes, false);
        var ipfsKey = Serializer.Deserialize<PublicKeyMessage>(ms);

        switch (ipfsKey.Type)
        {
            case KeyType.Rsa:
                key._publicKey = PublicKeyFactory.CreateKey(ipfsKey.Data);
                key._signingAlgorithmName = RsaSigningAlgorithmName;
                break;

            case KeyType.Ed25519:
                key._publicKey = PublicKeyFactory.CreateKey(ipfsKey.Data);
                key._signingAlgorithmName = Ed25519SigningAlgorithmName;
                break;

            case KeyType.Secp256K1:
                key._publicKey = PublicKeyFactory.CreateKey(ipfsKey.Data);
                key._signingAlgorithmName = EcSigningAlgorithmName;
                break;

            default:
                throw new InvalidDataException($"Unknown key type of {ipfsKey.Type}.");
        }

        return key;
    }

    /// <summary>
    ///   Create the key from the Bouncy Castle private key.
    /// </summary>
    /// <param name="privateKey">
    ///   The Bouncy Castle private key.
    /// </param>
    public static Key CreatePrivateKey(AsymmetricKeyParameter privateKey)
    {
        var key = new Key
        {
            _privateKey = privateKey
        };

        switch (privateKey)
        {
            // Get the public key from the private key.
            case RsaPrivateCrtKeyParameters rsa:
                key._publicKey = new RsaKeyParameters(false, rsa.Modulus, rsa.PublicExponent);
                key._signingAlgorithmName = RsaSigningAlgorithmName;
                break;
            case Ed25519PrivateKeyParameters ed:
                key._publicKey = ed.GeneratePublicKey();
                key._signingAlgorithmName = Ed25519SigningAlgorithmName;
                break;
            case ECPrivateKeyParameters ec:
            {
                var q = ec.Parameters.G.Multiply(ec.D);
                key._publicKey = new ECPublicKeyParameters(q, ec.Parameters);
                key._signingAlgorithmName = EcSigningAlgorithmName;
                break;
            }
        }
        if (key._publicKey == null)
            throw new NotSupportedException($"The key type {privateKey.GetType().Name} is not supported.");

        return key;
    }

    private enum KeyType
    {
        Rsa = 0,
        Ed25519 = 1,
        Secp256K1 = 2,
        Ecdh = 4,
    }

    [ProtoContract]
    private class PublicKeyMessage
    {
        [ProtoMember(1, IsRequired = true)]
        public KeyType Type { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public byte[] Data { get; set; }
    }

#if false
        [ProtoContract]
        class PrivateKeyMessage
        {
            [ProtoMember(1, IsRequired = true)]
            public KeyType Type;
            [ProtoMember(2, IsRequired = true)]
            public byte[] Data;
        }
#endif
}