using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using IpfsShipyard.Ipfs.Core;

namespace PeerTalk
{
    /// <summary>
    ///   A collection of filters that are not approved.
    /// </summary>
    /// <remarks>
    ///   Only targets that do match a filter will pass.
    /// </remarks>
    public class MultiAddressBlackList : ICollection<MultiAddress>, IPolicy<MultiAddress>
    {
        private readonly ConcurrentDictionary<MultiAddress, MultiAddress> filters = new();

        /// <inheritdoc />
        public bool IsAllowed(MultiAddress target)
        {
            return !filters.Any(kvp => Matches(kvp.Key, target));
        }

        private bool Matches(MultiAddress filter, MultiAddress target)
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
        public bool Contains(MultiAddress item) => filters.ContainsKey(item);

        /// <inheritdoc />
        public void CopyTo(MultiAddress[] array, int arrayIndex) => filters.Keys.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<MultiAddress> GetEnumerator() => filters.Keys.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => filters.Keys.GetEnumerator();
    }
}