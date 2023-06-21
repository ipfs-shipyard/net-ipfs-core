using System.Collections.Generic;

namespace Ipfs.CoreApi
{
    /// <summary>
    ///   The statistics for <see cref="IStatsApi.BitswapAsync"/>.
    /// </summary>
    public class BitswapData
    {
        /// <summary>
        ///   TODO: Unknown.
        /// </summary>
        public int ProvideBufLen { get; set; }

        /// <summary>
        ///   The content that is wanted.
        /// </summary>
        public IEnumerable<Cid>? Wantlist { get; set; }

        /// <summary>
        ///   The known peers.
        /// </summary>
        public IEnumerable<MultiHash>? Peers { get; set; }

        /// <summary>
        ///   The number of blocks sent by other peers.
        /// </summary>
        public ulong BlocksReceived { get; set; }

        /// <summary>
        ///   The number of bytes sent by other peers.
        /// </summary>
        public ulong DataReceived { get; set; }

        /// <summary>
        ///   The number of blocks sent to other peers.
        /// </summary>
        public ulong BlocksSent { get; set; }

        /// <summary>
        ///   The number of bytes sent to other peers.
        /// </summary>
        public ulong DataSent { get; set; }

        /// <summary>
        ///   The number of duplicate blocks sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        public ulong DupBlksReceived { get; set; }

        /// <summary>
        ///   The number of duplicate bytes sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        public ulong DupDataReceived { get; set; }
    }
}
