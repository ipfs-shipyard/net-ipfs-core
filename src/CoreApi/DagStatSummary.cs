using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ipfs.CoreApi
{
    /// <summary>
    /// The output of a DAG stat operation.
    /// </summary>
    /// <remarks>
    /// Adapted from <see href="https://github.com/ipfs/kubo/blob/f5b855550ca73acf5dd3a2001e2a7192cab7c249/core/commands/dag/dag.go#L326"/>
    /// </remarks>
    public record DagStatSummary
    {
        /// <summary>
        /// The size of redundant nodes in this Dag tree.
        /// </summary>
        /// <remarks>
        /// Docs sourced from <see href="https://github.com/ipfs/kubo/blob/f5b855550ca73acf5dd3a2001e2a7192cab7c249/core/commands/dag/stat.go#L60-L70"/>
        /// </remarks>
        [JsonIgnore]
        public ulong RedundantSize { get; set; }

        /// <summary>
        /// The number of unique CIDs in this Dag tree.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? UniqueBlocks { get; set; }

        /// <summary>
        /// The total size of all unique dag blocks in this tree.
        /// </summary>
        /// <remarks>
        /// Docs sourced from <see href="https://github.com/ipfs/kubo/blob/f5b855550ca73acf5dd3a2001e2a7192cab7c249/core/commands/dag/stat.go#L58"/>
        /// </remarks>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? TotalSize { get; set; }

        /// <summary>
        /// The difference between <see cref="RedundantSize"/> and <see cref="TotalSize"/>.
        /// </summary>
        /// <remarks>
        /// Docs sourced from code <see href="https://github.com/ipfs/kubo/blob/f5b855550ca73acf5dd3a2001e2a7192cab7c249/core/commands/dag/dag.go#L351" />
        /// </remarks>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong? SharedSize { get; set; }

        /// <summary>
        /// The ratio of <see cref="RedundantSize"/> to <see cref="TotalSize"/>.
        /// </summary>
        /// <remarks>
        /// Docs sourced from code <see href="https://github.com/ipfs/kubo/blob/f5b855550ca73acf5dd3a2001e2a7192cab7c249/core/commands/dag/dag.go#L351" />
        /// </remarks>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? Ratio { get; set; }

        /// <summary>
        /// If requested, the stats for each DAG in the tree.
        /// </summary>
        [JsonProperty("DagStats", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<DagStat>? DagStatsArray { get; set; }
    }

}
