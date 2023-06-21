namespace Ipfs.CoreApi
{
    /// <summary>
    ///   The statistics for <see cref="IStatsApi.BandwidthAsync"/>.
    /// </summary>
    public class BandwidthData
    {
        /// <summary>
        ///   The number of bytes received.
        /// </summary>
        public ulong TotalIn { get; set; }

        /// <summary>
        ///   The number of bytes sent.
        /// </summary>
        public ulong TotalOut { get; set; }

        /// <summary>
        ///   TODO
        /// </summary>
        public double RateIn { get; set; }

        /// <summary>
        ///   TODO
        /// </summary>
        public double RateOut { get; set; }
    }
}
