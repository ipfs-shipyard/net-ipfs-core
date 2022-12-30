using Ipfs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    /// <summary>
    ///   Manages a list of content that is provided by multiple peers.
    /// </summary>
    /// <remarks>
    ///   A peer is expected to provide content for at least <see cref="ProviderTTL"/>.
    ///   After this expires the provider is removed from the list.
    /// </remarks>
    public class ContentRouter : IDisposable
    {
        class ProviderInfo
        {
            /// <summary>
            ///   When the provider entry expires.
            /// </summary>
            public DateTime Expiry { get; set; }

            /// <summary>
            ///   The peer ID of the provider.
            /// </summary>
            public MultiHash PeerId { get; set; }
        }

        ConcurrentDictionary<string, List<ProviderInfo>> content = new ConcurrentDictionary<string, List<ProviderInfo>>();
        string Key(Cid cid) => "/providers/" + cid.Hash.ToBase32();

        /// <summary>
        ///   How long a provider is assumed to provide some content.
        /// </summary>
        /// <value>
        ///   Defaults to 24 hours (1 day).
        /// </value>
        public TimeSpan ProviderTTL { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        ///    Adds the <see cref="Cid"/> and <see cref="Peer"/> to the content routing system.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content that the <paramref name="provider"/> contains.
        /// </param>
        /// <param name="provider">
        ///   The peer ID that contains the <paramref name="cid"/>.
        /// </param>
        public void Add(Cid cid, MultiHash provider)
        {
            Add(cid, provider, DateTime.Now);
        }

        /// <summary>
        ///   Adds the <see cref="Cid"/> and <see cref="Peer"/> to the content
        ///   routing system at the specified <see cref="DateTime"/>.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content that the <paramref name="provider"/> contains.
        /// </param>
        /// <param name="provider">
        ///   The peer ID that contains the <paramref name="cid"/>.
        /// </param>
        /// <param name="now">
        ///   The local time that the <paramref name="provider"/> started to provide
        ///   the <paramref name="cid"/>.
        /// </param>
        public void Add(Cid cid, MultiHash provider, DateTime now)
        {
            var pi = new ProviderInfo
            {
                Expiry = now + ProviderTTL,
                PeerId = provider
            };

            content.AddOrUpdate(
                Key(cid),
                (key) => new List<ProviderInfo> { pi },
                (key, providers) =>
                {
                    var existing = providers
                        .Where(p => p.PeerId == provider)
                        .FirstOrDefault();
                    if (existing != null)
                    {
                        existing.Expiry = pi.Expiry;
                    }
                    else
                    {
                        providers.Add(pi);
                    }
                    return providers;
                });
        }

        /// <summary>
        ///   Gets the providers for the <see cref="Cid"/>.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content.
        /// </param>
        /// <returns>
        ///   A sequence of peer IDs (providers) that contain the <paramref name="cid"/>.
        /// </returns>
        public IEnumerable<MultiHash> Get(Cid cid)
        {
            if (!content.TryGetValue(Key(cid), out List<ProviderInfo> providers))
            {
                return Enumerable.Empty<MultiHash>();
            }

            return providers
                .Where(p => DateTime.Now < p.Expiry)
                .Select(p => p.PeerId);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

    }
}
