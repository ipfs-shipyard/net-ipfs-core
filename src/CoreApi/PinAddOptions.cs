namespace Ipfs.CoreApi
{
    /// <summary>
    ///   Options for pin add.
    /// </summary>
    public class PinAddOptions
    {
        /// <summary>
        ///   Optional name for created pin(s).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        ///   True to recursively pin links of the object; otherwise, false to only pin the specified object.
        ///   Default is true.
        /// </summary>
        public bool Recursive { get; set; } = true;
    }
}
