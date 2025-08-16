using System;

namespace Ipfs.CoreApi
{
    /// <summary>
    ///   The options when adding data to the IPFS file system.
    /// </summary>
    /// <seealso cref="IFileSystemApi"/>
    public class AddFileOptions
    {
        /// <summary>
        ///   Determines if the data is pinned to local storage.
        /// </summary>
        /// <value>
        ///   If <b>true</b> the data is pinned to local storage and will not be
        ///   garbage collected. Required: no. The default is <b>true</b>.
        /// </value>
        public bool? Pin { get; set; }

        /// <summary>
        ///   Optional name to assign to the pin when pinning during add (Kubo v0.37.0+).
        /// </summary>
        /// <remarks>
        ///   Forwarded as the 'pin-name' parameter to the add RPC. Effective when pinning is enabled.
        /// </remarks>
        public string? PinName { get; set; }

        /// <summary>
        ///   Chunking algorithm, size-[bytes], rabin-[min]-[avg]-[max] or buzhash. Required: no.
        /// </summary>
        /// <value>
        ///   Required: no. The default is 256 * 1024 (size-‭262144) bytes.‬
        /// </value>
        public string? Chunker { get; set; }

        /// <summary>
        ///   Determines if the trickle-dag format is used for dag generation.
        /// </summary>
        /// <value>
        ///   Required: no. The default is <b>false</b>.
        /// </value>
        public bool? Trickle { get; set; }

        /// <summary>
        ///   Determines if added file(s) are wrapped in a directory object.
        /// </summary>
        /// <value>
        ///   Required: no. The default is <b>false</b>.
        /// </value>
        public bool? Wrap { get; set; }

        /// <summary>
        ///   Determines if raw blocks are used for leaf nodes.
        /// </summary>
        /// <value>
        ///   Required: no. The default is <b>false</b>.
        /// </value>
        public bool? RawLeaves { get; set; }

        /// <summary>
        ///   The hashing algorithm name to use.
        /// </summary>
        /// <value>
        ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
        ///   Defaults to <see cref="MultiHash.DefaultAlgorithmName"/>.
        /// </value>
        /// <seealso cref="MultiHash"/>
        public string? Hash { get; set; }

        /// <summary>
        ///   Determines if only file information is produced.
        /// </summary>
        /// <value>
        ///   If <b>true</b> no data is added to IPFS. Required: no. The default is <b>false</b>.
        /// </value>
        public bool? OnlyHash { get; set; }

        /// <summary>
        ///   Used to report the progress of a file transfer.
        /// </summary>
        public IProgress<TransferProgress>? Progress { get; set; }

        /// <summary>
        /// Add the file using filestore. Implies raw-leaves.
        /// </summary>
        public bool? NoCopy { get; set; }

        /// <summary>
        /// Check the filestore for pre-existing blocks. 
        /// </summary>
        public bool? FsCache { get; set; }

        /// <summary>
        /// Defaults to 0 unless an option that depends on CIDv1 is passed.
        /// Passing version 1 will cause the raw-leaves option to default to true.
        /// Required: no.
        /// </summary>
        public int? CidVersion { get; set; }

        /// <summary>
        /// Inline small blocks into CIDs. (experimental). Required: no.
        /// </summary>
        public bool? Inline { get; set; }

        /// <summary>
        /// Maximum block size to inline. (experimental). Default: 32. Required: no.
        /// </summary>
        public int? InlineLimit { get; set; }

        /// <summary>
        /// Add reference to Files API (MFS) at the provided path. Required: no.
        /// </summary>
        public string? ToFiles { get; set; }

        /// <summary>
        /// Apply existing POSIX permissions to created UnixFS entries. Disables raw-leaves. (experimental). Required: no.
        /// </summary>
        public bool? PreserveMode { get; set; }

        /// <summary>
        /// Apply existing POSIX modification time to created UnixFS entries. Disables raw-leaves. (experimental). Required: no.
        /// </summary>
        public bool? PreserveMtime { get; set; }

        /// <summary>
        /// Custom POSIX file mode to store in created UnixFS entries. Disables raw-leaves. (experimental). Required: no.
        /// </summary>
        public uint? Mode { get; set; }

        /// <summary>
        /// Custom POSIX modification time to store in created UnixFS entries (seconds before or after the Unix Epoch). Disables raw-leaves. (experimental). Required: no.
        /// </summary>
        public long? Mtime { get; set; }

        /// <summary>
        /// Custom POSIX modification time (optional time fraction in nanoseconds).
        /// </summary>
        public uint? MtimeNsecs { get; set; }
    }
}
