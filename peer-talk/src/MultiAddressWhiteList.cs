using Ipfs;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   A sequence of filters that are approved.
    /// </summary>
    /// <remarks>
    ///   Only targets that are a subset of any filters will pass.  If no filters are defined, then anything
    ///   passes.
    /// </remarks>
    public class MultiAddressWhiteList : ICollection<MultiAddress>, IPolicy<MultiAddress>
    {
        ConcurrentDictionary<MultiAddress, MultiAddress> filters = new ConcurrentDictionary<MultiAddress, MultiAddress>();

        /// <inheritdoc />
        public bool IsAllowed(MultiAddress target)
        {
            if (filters.IsEmpty)
                return true;

            return filters.Any(kvp => Matches(kvp.Key, target));
        }

        bool Matches(MultiAddress filter, MultiAddress target)
        {
            return filter
                .Protocols
                .All(fp => target.Protocols.Any(tp => tp.Code == fp.Code && tp.Value == fp.Value));
        }

        /// <inheritdoc />
        public bool Remove(MultiAddress item) => filters.TryRemove(item, out _);

        /// <inheritdoc />
        public int Count => filters.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(MultiAddress item) => filters.TryAdd(item, item);

        /// <inheritdoc />
        public void Clear() => filters.Clear();

        /// <inheritdoc />
        public bool Contains(MultiAddress item) => filters.Keys.Contains(item);

        /// <inheritdoc />
        public void CopyTo(MultiAddress[] array, int arrayIndex) => filters.Keys.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<MultiAddress> GetEnumerator() => filters.Keys.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => filters.Keys.GetEnumerator();
    }
}
