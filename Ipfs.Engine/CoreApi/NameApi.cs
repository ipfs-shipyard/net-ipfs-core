using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi;

internal class NameApi : INameApi
{
    private readonly IpfsEngine _ipfs;

    public NameApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public Task<NamedContent> PublishAsync(string path, bool resolve = true, string key = "self",
        TimeSpan? lifetime = null, CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }

    public Task<NamedContent> PublishAsync(Cid id, string key = "self", TimeSpan? lifetime = null,
        CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string> ResolveAsync(string name, bool recursive = false, bool nocache = false,
        CancellationToken cancel = default)
    {
        do
        {
            if (name.StartsWith("/ipns/"))
            {
                name = name[6..];
            }

            var parts = name.Split('/').Where(p => p.Length > 0).ToArray();
            if (parts.Length == 0)
            {
                throw new ArgumentException($"Cannot resolve '{name}'.");
            }

            if (IsDomainName(parts[0]))
            {
                name = await _ipfs.Dns.ResolveAsync(parts[0], recursive, cancel).ConfigureAwait(false);
            }
            else
            {
                throw new NotImplementedException("Resolving IPNS is not yet implemented.");
            }

            name = parts.Length > 1 ? name + "/" + string.Join("/", parts, 1, parts.Length - 1) : name;
        } while (recursive && !name.StartsWith("/ipfs/"));

        return name;
    }

    /// <summary>
    ///     Determines if the supplied string is a valid domain name.
    /// </summary>
    /// <param name="name">
    ///     An domain name, such as "ipfs.io".
    /// </param>
    /// <returns>
    ///     <b>true</b> if <paramref name="name" /> is a domain name;
    ///     otherwise, <b>false</b>.
    /// </returns>
    /// <remarks>
    ///     A domain must contain at least one '.'.
    /// </remarks>
    public static bool IsDomainName(string name) => name.IndexOf('.') > 0;
}