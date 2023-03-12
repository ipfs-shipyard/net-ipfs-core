using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.PeerTalk;

/// <summary>
///   A collection of filters that are not approved.
/// </summary>
/// <remarks>
///   Only targets that do match a filter will pass.
/// </remarks>
public class MultiAddressBlackList : ICollection<MultiAddress>, IPolicy<MultiAddress>
{
    private readonly ConcurrentDictionary<MultiAddress, MultiAddress> _filters = new();

    /// <inheritdoc />
    public bool IsAllowed(MultiAddress target)
    {
        return !_filters.Any(kvp => Matches(kvp.Key, target));
    }

    private bool Matches(MultiAddress filter, MultiAddress target)
    {
        return filter
            .Protocols
            .All(fp => target.Protocols.Any(tp => tp.Code == fp.Code && tp.Value == fp.Value));
    }

    /// <inheritdoc />
    public bool Remove(MultiAddress item) => _filters.TryRemove(item, out _);

    /// <inheritdoc />
    public int Count => _filters.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(MultiAddress item) => _filters.TryAdd(item, item);

    /// <inheritdoc />
    public void Clear() => _filters.Clear();

    /// <inheritdoc />
    public bool Contains(MultiAddress item) => _filters.ContainsKey(item);

    /// <inheritdoc />
    public void CopyTo(MultiAddress[] array, int arrayIndex) => _filters.Keys.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<MultiAddress> GetEnumerator() => _filters.Keys.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _filters.Keys.GetEnumerator();
}