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
    public class ContentProviders : IDisposable
    {
        ConcurrentDictionary<string, List<MultiHash>> content = new ConcurrentDictionary<string, List<MultiHash>>();
        string Key(Cid cid) => "/provider/" + cid.Hash.ToBase32();

        /// <summary>
        ///    Adds the <see cref="Cid"/> and <see cref="Peer"/> to the content routing system.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content that the <paramref name="provider"/> contains.
        /// </param>
        /// <param name="provider">
        ///   The peer ID that contains the <paramref name="cid"/>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public Task AddAsync(Cid cid, MultiHash provider, CancellationToken cancel = default(CancellationToken))
        {
            content.AddOrUpdate(
                Key(cid),
                (key) => new List<MultiHash> { provider },
                (key, providers) =>
                {
                    if (!providers.Contains(provider))
                    {
                        providers.Add(provider);
                    }
                    return providers;
                });
            return Task.CompletedTask;
        }

        /// <summary>
        ///   Gets the providers for the <see cref="Cid"/>.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation that returns
        ///   a sequence of peer IDs (providers) that contain the <paramref name="cid"/>.
        /// </returns>
        public Task<IEnumerable<MultiHash>> GetAsync(Cid cid, CancellationToken cancel = default(CancellationToken))
        { 
            if (content.TryGetValue(Key(cid), out List<MultiHash> providers))
            {
                return Task.FromResult(providers.AsEnumerable());
            }

            return Task.FromResult(Enumerable.Empty<MultiHash>());
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }


    }
}
