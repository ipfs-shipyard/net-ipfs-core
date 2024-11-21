using System.IO;

namespace Ipfs.CoreApi
{
    /// <summary>
    /// Represents a file to be added to IPFS.
    /// </summary>
    public class FilePart
    {
        /// <summary>
        /// The name of the file or the relative path to the file in a folder.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// A stream containing the file's data.
        /// </summary>
        public required Stream? Data { get; set; }

        /// <summary>
        /// Can be set to the location of the file in the filesystem (within the IPFS root), or to its full web URL.
        /// </summary>
        /// <remarks>
        ///  Included for filestore/urlstore features that are enabled with the nocopy option
        /// </remarks>
        public string? AbsolutePath { get; set; }
    }
}
