using System;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Microsoft.AspNetCore.Mvc;

namespace IpfsShipyard.Ipfs.Server.HttpApi.V0;

/// <summary>
///     Content that has an associated name.
/// </summary>
public class NamedContentDto
{
    /// <summary>
    ///     Path to the name, "/ipns/...".
    /// </summary>
    public string Name;

    /// <summary>
    ///     Path to the content, "/ipfs/...".
    /// </summary>
    public string Value;
}

/// <summary>
///     Manages the IPNS (Interplanetary Name Space).
/// </summary>
/// <remarks>
///     IPNS is a PKI namespace, where names are the hashes of public keys, and
///     the private key enables publishing new(signed) values. The default name
///     is the node's own <see cref="Peer.Id" />,
///     which is the hash of its public key.
/// </remarks>
public class NameController : IpfsController
{
    /// <summary>
    ///     Creates a new controller.
    /// </summary>
    public NameController(ICoreApi ipfs) : base(ipfs)
    {
    }

    /// <summary>
    ///     Resolve a name.
    /// </summary>
    [HttpGet]
    [HttpPost]
    [Route("name/resolve")]
    public async Task<PathDto> Resolve(
        string arg,
        bool recursive = false,
        bool nocache = false)
    {
        var path = await IpfsCore.Name.ResolveAsync(arg, recursive, nocache, Cancel);
        return new(path);
    }

    /// <summary>
    ///     Publish content.
    /// </summary>
    /// <param name="arg">
    ///     The CID or path to the content to publish.
    /// </param>
    /// <param name="resolve">
    ///     Resolve before publishing.
    /// </param>
    /// <param name="key">
    ///     The local key name used to sign the content.
    /// </param>
    /// <param name="lifetime">
    ///     Duration that the record will be valid for.
    /// </param>
    [HttpGet]
    [HttpPost]
    [Route("name/publish")]
    public async Task<NamedContentDto> Publish(
        string arg,
        bool resolve = true,
        string key = "self",
        string lifetime = "24h")
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            throw new ArgumentNullException(nameof(arg), "The name is required.");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "The key name is required.");
        }

        if (string.IsNullOrWhiteSpace(lifetime))
        {
            throw new ArgumentNullException(nameof(lifetime), "The lifetime is required.");
        }

        var duration = Duration.Parse(lifetime);
        var content = await IpfsCore.Name.PublishAsync(arg, resolve, key, duration, Cancel);
        return new()
        {
            Name = content.NamePath,
            Value = content.ContentPath
        };
    }
}