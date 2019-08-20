using Ipfs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   A sequence of filters that are not approved.
    /// </summary>
    /// <remarks>
    ///   Only targets that do match a filter will pass.
    /// </remarks>
    public class MultiAddressBlackList : List<MultiAddress>, IPolicy<MultiAddress>
    {
        /// <inheritdoc />
        public bool IsAllowed(MultiAddress target)
        {
            return !this.Any(filter => Matches(filter, target));
        }

        bool Matches(MultiAddress filter, MultiAddress target)
        {
            return filter
                .Protocols
                .All(fp => target.Protocols.Any(tp => tp.Code == fp.Code && tp.Value == fp.Value));
        }

    }
}
