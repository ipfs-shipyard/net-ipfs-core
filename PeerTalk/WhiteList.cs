using System;
using System.Collections.Concurrent;
using System.Linq;

namespace IpfsShipyard.PeerTalk
{
    /// <summary>
    ///   A sequence of targets that are approved.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    /// <remarks>
    ///   Only targets that are defined will pass.  If no targets are defined, then anything
    ///   passes.
    /// </remarks>
    public class WhiteList<T> : ConcurrentBag<T>, IPolicy<T>
        where T : IEquatable<T>
    {
        /// <inheritdoc />
        public bool IsAllowed(T target)
        {
            return this.IsEmpty || this.Contains(target);
        }
    }
}
