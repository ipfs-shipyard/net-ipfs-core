#nullable enable
using System;

namespace Ipfs.CoreApi
{
    /// <summary>
    ///   Information on a MFS file path
    /// </summary>
    /// <seealso cref="IMfsApi.StatAsync(string, System.Threading.CancellationToken)"/>
    public class FileStatResult
    {
        /// <summary>
        /// The <see cref="Cid"/> of the file or directory.
        /// </summary>
        public Cid? Hash { get; set; }

        /// <summary>
        ///   The serialised size (in bytes) of the linked node.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        ///   Size of object and its references.
        /// </summary>
        public long CumulativeSize { get; set; }

        /// <summary>
        ///   Determines if the node is a directory (folder).
        /// </summary>
        /// <value>
        ///   <b>true</b> if the node is a directory; Otherwise <b>false</b>,
        ///   it is some type of a file.
        /// </value>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Number of blocks
        /// </summary>
        public int Blocks { get; set; }

        /// <summary>
        ///   The modification time of the file/directory.
        /// </summary>
        /// <summary>
        ///   The modification time in seconds.
        /// </summary>
        public long? Mtime { get; set; }

        /// <summary>
        ///   The modification time fraction in nanoseconds.
        /// </summary>
        public int? MtimeNsecs { get; set; }

        /// <summary>
        ///   The file mode.
        /// </summary>
        /// <summary>
        ///   The file mode.
        /// </summary>
        public string? Mode { get; set; }
    }

    /// <summary>
    ///   Expanded <see cref="FileStatResult"/> result when parameter is-local=true is used.
    /// </summary>
    /// <seealso cref="IMfsApi.StatAsync(string, bool, System.Threading.CancellationToken)"/>
    public class FileStatWithLocalityResult : FileStatResult
    {
        /// <summary>
        ///   Is local object.
        /// </summary>
        public bool Local { get; set; }

        /// <summary>
        ///   The serialised size (in bytes) of the linked node that is local.
        /// </summary>
        public long SizeLocal { get; set; }

        /// <summary>
        ///   Reflection of the parameter is-local.
        /// </summary>
        public bool WithLocality { get; set; }
    }
}



