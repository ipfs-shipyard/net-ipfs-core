using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Makaretu.Dns;
using PeerTalk;

namespace Ipfs.Engine.CoreApi;

internal class DnsApi : IDnsApi
{
    private readonly IpfsEngine _ipfs;

    public DnsApi(IpfsEngine ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task<string> ResolveAsync(string name, bool recursive = false, CancellationToken cancel = default)
    {
        // Find the TXT dnslink in either <name> or _dnslink.<name>.
        string link;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        try
        {
            var attempts = new[]
            {
                FindAsync(name, cts.Token),
                FindAsync("_dnslink." + name, cts.Token)
            };
            link = await TaskHelper.WhenAnyResultAsync(attempts, cancel).ConfigureAwait(false);
            cts.Cancel();
        }
        catch (Exception e)
        {
            throw new NotSupportedException($"Cannot resolve '{name}'.", e);
        }

        if (!recursive || link.StartsWith("/ipfs/"))
        {
            return link;
        }

        if (link.StartsWith("/ipns/"))
        {
            return await _ipfs.Name.ResolveAsync(link, true, false, cancel).ConfigureAwait(false);
        }

        throw new NotSupportedException($"Cannot resolve '{link}'.");
    }

    private async Task<string> FindAsync(string name, CancellationToken cancel)
    {
        var response = await _ipfs.Options.Dns.QueryAsync(name, DnsType.TXT, cancel).ConfigureAwait(false);
        var link = response.Answers
            .OfType<TXTRecord>()
            .SelectMany(txt => txt.Strings)
            .Where(s => s.StartsWith("dnslink="))
            .Select(s => s[8..])
            .FirstOrDefault();

        return link ?? throw new($"'{name}' is missing a TXT record with a dnslink.");
    }
}