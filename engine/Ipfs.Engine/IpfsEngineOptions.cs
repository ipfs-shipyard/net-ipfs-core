using Ipfs.Engine.Cryptography;
using Makaretu.Dns;

namespace Ipfs.Engine;

/// <summary>
///     Configuration options for the <see cref="IpfsEngine" />.
/// </summary>
/// <seealso cref="IpfsEngine.Options" />
public class IpfsEngineOptions
{
    /// <summary>
    ///     Repository options.
    /// </summary>
    public RepositoryOptions Repository { get; set; } = new();

    /// <summary>
    ///     KeyChain options.
    /// </summary>
    public KeyChainOptions KeyChain { get; set; } = new();

    /// <summary>
    ///     Provides access to the Domain Name System.
    /// </summary>
    /// <value>
    ///     Defaults to <see cref="DotClient" />, DNS over TLS.
    /// </value>
    public IDnsClient Dns { get; set; } = new DotClient();

    /// <summary>
    ///     Block options.
    /// </summary>
    public BlockOptions Block { get; set; } = new();

    /// <summary>
    ///     Discovery options.
    /// </summary>
    public DiscoveryOptions Discovery { get; set; } = new();

    /// <summary>
    ///     Swarm (network) options.
    /// </summary>
    public SwarmOptions Swarm { get; set; } = new();
}