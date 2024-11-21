using Newtonsoft.Json;

namespace Ipfs.CoreApi
{
    /// <summary>
    /// Statistics about a DAG block.
    /// </summary>
    public record DagStat
    {
        /// <summary>
        /// The CID of the block.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DagCid? Cid { get; set; }

        /// <summary>
        /// The size of the block.
        /// </summary>
        public ulong? Size { get; set; }

        /// <summary>
        /// The number of links in the block.
        /// </summary>
        public long NumBlocks { get; set; }
    }

}
