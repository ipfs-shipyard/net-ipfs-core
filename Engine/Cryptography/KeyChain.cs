using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using IpfsShipyard.Ipfs.Engine.Cryptography.Proto;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using ProtoBuf;

namespace IpfsShipyard.Ipfs.Engine.Cryptography;

/// <summary>
///     A secure key chain.
/// </summary>
public partial class KeyChain : IKeyApi
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(KeyChain));

    private readonly IpfsEngine _ipfs;
    private char[] _dek;
    private FileStore<string, EncryptedKey> _store;

    /// <summary>
    ///     Create a new instance of the <see cref="KeyChain" /> class.
    /// </summary>
    /// <param name="ipfs">
    ///     The IPFS Engine associated with the key chain.
    /// </param>
    public KeyChain(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    private FileStore<string, EncryptedKey> Store
    {
        get
        {
            if (_store != null)
            {
                return _store;
            }

            var folder = Path.Combine(_ipfs.Options.Repository.Folder, "keys");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _store = new()
            {
                Folder = folder,
                NameToKey = name => Encoding.UTF8.GetBytes(name).ToBase32(),
                KeyToName = key => Encoding.UTF8.GetString(Base32.Decode(key))
            };

            return _store;
        }
    }

    /// <summary>
    ///     The configuration options.
    /// </summary>
    public KeyChainOptions Options { get; set; } = new();

    /// <inheritdoc />
    public async Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default)
    {
        // Apply defaults.
        if (string.IsNullOrWhiteSpace(keyType))
        {
            keyType = Options.DefaultKeyType;
        }

        size = size < 1 ? Options.DefaultKeySize : size;

        keyType = keyType.ToLowerInvariant();

        // Create the key pair.
        Log.DebugFormat("Creating {0} key named '{1}'", keyType, name);
        IAsymmetricCipherKeyPairGenerator g;
        switch (keyType)
        {
            case "rsa":
                g = GeneratorUtilities.GetKeyPairGenerator("RSA");
                g.Init(new RsaKeyGenerationParameters(
                    BigInteger.ValueOf(0x10001), new(), size, 25));
                break;
            case "ed25519":
                g = GeneratorUtilities.GetKeyPairGenerator("Ed25519");
                g.Init(new Ed25519KeyGenerationParameters(new()));
                break;
            case "secp256k1":
                g = GeneratorUtilities.GetKeyPairGenerator("EC");
                g.Init(new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256k1, new()));
                break;
            default:
                throw new($"Invalid key type '{keyType}'.");
        }

        var keyPair = g.GenerateKeyPair();
        Log.Debug("Created key");

        return await AddPrivateKeyAsync(name, keyPair, cancel).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default)
    {
        var pem = "";
        var key = await Store.GetAsync(name, cancel).ConfigureAwait(false);
        UseEncryptedKey(key, pkey =>
        {
            using var sw = new StringWriter();
            var pkcs8 = new Pkcs8Generator(pkey, Pkcs8Generator.PbeSha1_3DES)
            {
                Password = password
            };
            var pw = new PemWriter(sw);
            pw.WriteObject(pkcs8);
            pw.Writer.Flush();
            pem = sw.ToString();
        });

        return pem;
    }

    /// <inheritdoc />
    public async Task<IKey> ImportAsync(string name, string pem, char[] password = null,
        CancellationToken cancel = default)
    {
        AsymmetricKeyParameter key;
        using var sr = new StringReader(pem);
        using var pf = new PasswordFinder { Password = password };
        var reader = new PemReader(sr, pf);
        try
        {
            key = reader.ReadObject() as AsymmetricKeyParameter;
        }
        catch (Exception e)
        {
            throw new UnauthorizedAccessException("The password is wrong.", e);
        }

        if (key == null || !key.IsPrivate)
        {
            throw new InvalidDataException("Not a valid PEM private key");
        }

        return await AddPrivateKeyAsync(name, GetKeyPairFromPrivateKey(key), cancel).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default)
    {
        var keys = Store
                .Values
                .Select(key => (IKey)new KeyInfo { Id = key.Id, Name = key.Name })
            ;
        return Task.FromResult(keys);
    }

    /// <inheritdoc />
    public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default)
    {
        var key = await Store.TryGetAsync(name, cancel).ConfigureAwait(false);
        if (key == null)
        {
            return null;
        }

        await Store.RemoveAsync(name, cancel).ConfigureAwait(false);
        return new KeyInfo { Id = key.Id, Name = key.Name };
    }

    /// <inheritdoc />
    public async Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default)
    {
        var key = await Store.TryGetAsync(oldName, cancel).ConfigureAwait(false);
        if (key == null)
        {
            return null;
        }

        key.Name = newName;
        await Store.PutAsync(newName, key, cancel).ConfigureAwait(false);
        await Store.RemoveAsync(oldName, cancel).ConfigureAwait(false);

        return new KeyInfo { Id = key.Id, Name = newName };
    }

    /// <summary>
    ///     Sets the passphrase for the key chain.
    /// </summary>
    /// <param name="passphrase"></param>
    /// <param name="cancel">
    ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    ///     When the <paramref name="passphrase" /> is wrong.
    /// </exception>
    /// <remarks>
    ///     The <paramref name="passphrase" /> is used to generate a DEK (derived encryption
    ///     key).  The DEK is then used to encrypt the stored keys.
    ///     <para>
    ///         Neither the <paramref name="passphrase" /> nor the DEK are stored.
    ///     </para>
    /// </remarks>
    public async Task SetPassphraseAsync(SecureString passphrase, CancellationToken cancel = default)
    {
        // TODO: Verify DEK options.
        // TODO: get digest based on Options.Hash
        passphrase.UseSecretBytes(plain =>
        {
            var pdb = new Pkcs5S2ParametersGenerator(new Sha256Digest());
            pdb.Init(
                plain,
                Encoding.UTF8.GetBytes(Options.Dek.Salt),
                Options.Dek.IterationCount);
            var key = (KeyParameter)pdb.GenerateDerivedMacParameters(Options.Dek.KeyLength * 8);
            _dek = key.GetKey().ToBase64NoPad().ToCharArray();
        });

        // Verify that that pass phrase is okay, by reading a key.
        var akey = await Store.TryGetAsync("self", cancel).ConfigureAwait(false);
        if (akey != null)
        {
            try
            {
                UseEncryptedKey(akey, _ => { });
            }
            catch (Exception e)
            {
                throw new UnauthorizedAccessException("The pass phrase is wrong.", e);
            }
        }

        Log.Debug("Pass phrase is okay");
    }

    /// <summary>
    ///     Find a key by its name.
    /// </summary>
    /// <param name="name">
    ///     The local name of the key.
    /// </param>
    /// <param name="cancel">
    ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task's result is
    ///     an <see cref="IKey" /> or <b>null</b> if the the key is not defined.
    /// </returns>
    public async Task<IKey> FindKeyByNameAsync(string name, CancellationToken cancel = default)
    {
        var key = await Store.TryGetAsync(name, cancel).ConfigureAwait(false);
        return key == null ? null : new KeyInfo { Id = key.Id, Name = key.Name };
    }

    /// <summary>
    ///     Gets the IPFS encoded public key for the specified key.
    /// </summary>
    /// <param name="name">
    ///     The local name of the key.
    /// </param>
    /// <param name="cancel">
    ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task's result is
    ///     the IPFS encoded public key.
    /// </returns>
    /// <remarks>
    ///     The IPFS public key is the base-64 encoding of a protobuf encoding containing
    ///     a type and the DER encoding of the PKCS Subject Public Key Info.
    /// </remarks>
    /// <seealso href="https://tools.ietf.org/html/rfc5280#section-4.1.2.7" />
    public async Task<string> GetPublicKeyAsync(string name, CancellationToken cancel = default)
    {
        // TODO: Rename to GetIpfsPublicKeyAsync
        string result = null;
        var ekey = await Store.TryGetAsync(name, cancel).ConfigureAwait(false);
        if (ekey != null)
        {
            UseEncryptedKey(ekey, key =>
            {
                var kp = GetKeyPairFromPrivateKey(key);
                var spki = SubjectPublicKeyInfoFactory
                    .CreateSubjectPublicKeyInfo(kp.Public)
                    .GetDerEncoded();
                // Add protobuf cruft.
                var publicKey = new PublicKey
                {
                    Data = spki,
                    Type = kp.Public switch
                    {
                        RsaKeyParameters => KeyType.Rsa,
                        Ed25519PublicKeyParameters => KeyType.Ed25519,
                        ECPublicKeyParameters => KeyType.Secp256K1,
                        _ => throw new NotSupportedException($"The key type {kp.Public.GetType().Name} is not supported.")
                    }
                };

                using var ms = new MemoryStream();
                Serializer.Serialize(ms, publicKey);
                result = Convert.ToBase64String(ms.ToArray());
            });
        }

        return result;
    }

    /// <summary>
    ///     Gets the Bouncy Castle representation of the private key.
    /// </summary>
    /// <param name="name">
    ///     The local name of key.
    /// </param>
    /// <param name="cancel">
    ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task's result is
    ///     the private key as an <b>AsymmetricKeyParameter</b>.
    /// </returns>
    public async Task<AsymmetricKeyParameter> GetPrivateKeyAsync(string name, CancellationToken cancel = default)
    {
        var key = await Store.TryGetAsync(name, cancel).ConfigureAwait(false);
        if (key == null)
        {
            throw new KeyNotFoundException($"The key '{name}' does not exist.");
        }

        AsymmetricKeyParameter kp = null;
        UseEncryptedKey(key, pkey => { kp = pkey; });
        return kp;
    }

    private void UseEncryptedKey(EncryptedKey key, Action<AsymmetricKeyParameter> action)
    {
        using var sr = new StringReader(key.Pem);
        using var pf = new PasswordFinder { Password = _dek };
        var reader = new PemReader(sr, pf);
        var privateKey = (AsymmetricKeyParameter)reader.ReadObject();
        action(privateKey);
    }

    private async Task<IKey> AddPrivateKeyAsync(string name, AsymmetricCipherKeyPair keyPair, CancellationToken cancel)
    {
        // Create the key ID
        var keyId = CreateKeyId(keyPair.Public);

        // Create the PKCS #8 container for the key
        await using var sw = new StringWriter();
        var pkcs8 = new Pkcs8Generator(keyPair.Private, Pkcs8Generator.PbeSha1_3DES)
        {
            Password = _dek
        };
        var pw = new PemWriter(sw);
        pw.WriteObject(pkcs8);
        await pw.Writer.FlushAsync();
        var pem = sw.ToString();

        // Store the key in the repository.
        var key = new EncryptedKey
        {
            Id = keyId.ToBase58(),
            Name = name,
            Pem = pem
        };
        await Store.PutAsync(name, key, cancel).ConfigureAwait(false);
        Log.DebugFormat("Added key '{0}' with ID {1}", name, keyId);

        return new KeyInfo { Id = key.Id, Name = key.Name };
    }

    /// <summary>
    ///     Create a key ID for the key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <remarks>
    ///     The key id is the SHA-256 multihash of its public key. The public key is
    ///     a protobuf encoding containing a type and
    ///     the DER encoding of the PKCS SubjectPublicKeyInfo.
    /// </remarks>
    private static MultiHash CreateKeyId(AsymmetricKeyParameter key)
    {
        var spki = SubjectPublicKeyInfoFactory
            .CreateSubjectPublicKeyInfo(key)
            .GetDerEncoded();

        // Add protobuf cruft.
        var publicKey = new PublicKey
        {
            Data = spki,
            Type = key switch
            {
                RsaKeyParameters => KeyType.Rsa,
                ECPublicKeyParameters => KeyType.Secp256K1,
                Ed25519PublicKeyParameters => KeyType.Ed25519,
                _ => throw new NotSupportedException($"The key type {key.GetType().Name} is not supported.")
            }
        };

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, publicKey);

        // If the length of the serialized bytes <= 42, then we compute the "identity" multihash of 
        // the serialized bytes. The idea here is that if the serialized byte array 
        // is short enough, we can fit it in a multihash verbatim without having to 
        // condense it using a hash function.
        var alg = ms.Length <= 48 ? "identity" : "sha2-256";

        ms.Position = 0;
        return MultiHash.ComputeHash(ms, alg);
    }

    private static AsymmetricCipherKeyPair GetKeyPairFromPrivateKey(AsymmetricKeyParameter privateKey)
    {
        AsymmetricCipherKeyPair keyPair = null;
        switch (privateKey)
        {
            case RsaPrivateCrtKeyParameters rsa:
            {
                var pub = new RsaKeyParameters(false, rsa.Modulus, rsa.PublicExponent);
                keyPair = new(pub, privateKey);
                break;
            }
            case Ed25519PrivateKeyParameters ed:
            {
                var pub = ed.GeneratePublicKey();
                keyPair = new(pub, privateKey);
                break;
            }
            case ECPrivateKeyParameters ec:
            {
                var q = ec.Parameters.G.Multiply(ec.D);
                var pub = new ECPublicKeyParameters(ec.AlgorithmName, q, ec.PublicKeyParamSet);
                keyPair = new(pub, ec);
                break;
            }
        }

        return keyPair ?? throw new NotSupportedException($"The key type {privateKey.GetType().Name} is not supported.");
    }

    private class PasswordFinder : IPasswordFinder, IDisposable
    {
        public char[] Password;

        public void Dispose()
        {
            Password = null;
        }

        public char[] GetPassword()
        {
            return Password;
        }
    }
}