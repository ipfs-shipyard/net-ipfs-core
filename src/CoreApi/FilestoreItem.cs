namespace Ipfs.CoreApi
{
    /// <summary>
    /// A record that represents an object in a <see cref="IFilestoreApi"/> response.
    /// </summary>
    public record FilestoreItem
    {
        /// <summary>
        /// The key of the object.
        /// </summary>
        public required DagCid Key { get; set; }

        /// <summary>
        /// Holds any error message.
        /// </summary>
        public required string ErrorMsg { get; set; }

        /// <summary>
        /// Path to the file
        /// </summary>
        public required string FilePath { get; set; }

        /// <summary>
        /// The response offset.
        /// </summary>
        public required ulong Offset { get; set; }

        /// <summary>
        /// The size of the object.
        /// </summary>
        public ulong Size { get; set; }

        /// <summary>
        /// The object status.
        /// </summary>
        public int Status { get; set; }
    }
}
