using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   Maintains a timed cache of message IDs.
    /// </summary>
    /// <remarks>
    ///   <see cref="RecentlySeen(string, DateTime?)"/> can be used to detect duplicate
    ///   messages based on its ID.
    /// </remarks>
    public class MessageTracker
    {
        /// <summary>
        ///   The tracked messages.
        /// </summary>
        /// <remarks>
        ///   The key is the ID of a message.  The value is the expiry date.
        /// </remarks>
        ConcurrentDictionary<string, DateTime> messages = new ConcurrentDictionary<string, DateTime>();

        /// <summary>
        ///   The definition of recent.
        /// </summary>
        /// <value>
        ///   Defaults to 10 minutes.
        /// </value>
        /// <remarks>
        ///   Messages that have been in the cache longer that this value
        ///   will be removed and then be considered as "not seen".
        /// </remarks>
        public TimeSpan Recent { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        ///   Determines if the message has recently been seen.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of a message.
        /// </param>
        /// <param name="now"></param>
        /// <returns>
        ///   <b>true</b> if the <paramref name="id"/> has been recently seen;
        ///   otherwise, <b>false</b>.
        /// </returns>
        public bool RecentlySeen(string id, DateTime? now = null)
        {
            now = now ?? DateTime.Now;
            Prune(now.Value);

            var seen = false;
            messages.AddOrUpdate(
                id,
                (key) => now.Value + Recent,
                (key, expiry) => { seen = true; return now.Value + Recent; });

            return seen;
        }

        /// <summary>
        ///   Removes any message that is past its expiry date.
        /// </summary>
        /// <param name="now">
        ///   The current clock time.
        /// </param>
        public void Prune(DateTime now)
        {
            var expired = messages
                .Where(e => now >= e.Value)
                .Select(e => e.Key)
                .ToArray();
            foreach (var key in expired)
            {
                messages.TryRemove(key, out DateTime _);
            }
        }
    }
}
