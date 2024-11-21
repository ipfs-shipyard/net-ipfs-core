using Newtonsoft.Json;

namespace Ipfs.CoreApi
{
    /// <summary>
    /// CarImportOutput is the output type of the 'dag import' commands
    /// </summary>
    /// <remarks>See also <see href="https://github.com/ipfs/kubo/blob/f5b855550ca73acf5dd3a2001e2a7192cab7c249/core/commands/dag/dag.go#L63"/></remarks>
    public class CarImportOutput
    {
        /// <summary>
        /// Root is the metadata for a root pinning response
        /// </summary>
        [JsonProperty("Root", NullValueHandling = NullValueHandling.Ignore)]
        public RootMeta? Root { get; set; }

        /// <summary>
        /// Stats contains statistics about the imported CAR file, if requested.
        /// </summary>
        [JsonProperty("Stats", NullValueHandling = NullValueHandling.Ignore)]
        public CarImportStats? Stats { get; set; }

        /// <summary>
        /// RootMeta is the metadata for a root pinning response
        /// </summary>
        public record RootMeta
        {
            /// <summary>
            /// The CID of the root of the imported DAG.
            /// </summary>
            public required DagCid Cid { get; set; }

            /// <summary>
            /// The error message if pinning failed
            /// </summary>
            public string? PinErrorMsg { get; set; }
        }

        /// <summary>
        /// Statistics about an imported CAR file.
        /// </summary>
        public record CarImportStats
        {
            /// <summary>
            /// The number of blocks in the CAR file.
            /// </summary>
            [JsonProperty("BlockCount")]
            public ulong BlockCount { get; set; }

            /// <summary>
            /// The number of block bytes in the CAR file.
            /// </summary>
            [JsonProperty("BlockBytesCount")]
            public ulong BlockBytesCount { get; set; }
        }
    }


}
