#nullable enable
namespace Ipfs
{
    /// <summary>
    ///   Represents a decoded UnixFS DAG node.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This interface aligns with the UnixFS specification (https://specs.ipfs.tech/unixfs/).
    ///   A UnixFS node is a dag-pb protobuf whose <c>Data</c> field contains a nested UnixFSV1 protobuf message.
    ///   </para>
    ///   <para>
    ///   The optional <see cref="Mode"/> and <see cref="Mtime"/>/<see cref="MtimeNsecs"/> fields were added
    ///   in UnixFS 1.5 to support POSIX-like metadata. Per the spec (section 3.1.6):
    ///   <list type="bullet">
    ///     <item>These fields are opt-in and may be null if not set on the underlying DAG node.</item>
    ///     <item>Implementations MUST be able to parse UnixFS nodes both with and without these fields.</item>
    ///     <item>When present during operations like copying, implementations SHOULD preserve these fields.</item>
    ///   </list>
    ///   </para>
    ///   <para>
    ///   <b>Important:</b> Listing APIs (e.g., <c>files/ls</c>, <c>ipfs ls</c>) iterate the <c>PBNode.Links</c>
    ///   array without decoding each child node's UnixFS <c>Data</c> blob. As a result, <see cref="Mode"/>,
    ///   <see cref="Mtime"/>, and <see cref="MtimeNsecs"/> will be <c>null</c> when populated from listing operations.
    ///   To retrieve these values, use <c>files/stat</c> or decode the node directly via <c>dag/get</c>.
    ///   </para>
    /// </remarks>
    public interface IFileSystemNode : IMerkleNode<IFileSystemLink>
    {
        /// <summary>
        ///   Determines if the node is a directory (folder).
        /// </summary>
        /// <value>
        ///   <b>true</b> if the node is a directory; Otherwise <b>false</b>,
        ///   it is some type of a file.
        /// </value>
        bool IsDirectory { get; }

        /// <summary>
        ///   The file name of the IPFS node.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///   The optional POSIX file mode (UnixFS 1.5).
        /// </summary>
        /// <remarks>
        ///   Stored as an octal string (e.g., "0644") when returned by Kubo's <c>files/stat</c> API.
        ///   Will be <c>null</c> if not set on the underlying DAG node, or if populated from a listing API
        ///   that does not decode the UnixFS <c>Data</c> blob.
        /// </remarks>
        string? Mode { get; }

        /// <summary>
        ///   The optional modification time in seconds since Unix epoch (UnixFS 1.5).
        /// </summary>
        /// <remarks>
        ///   Corresponds to <c>UnixTime.Seconds</c> in the UnixFS protobuf schema.
        ///   Will be <c>null</c> if not set on the underlying DAG node, or if populated from a listing API
        ///   that does not decode the UnixFS <c>Data</c> blob.
        /// </remarks>
        long? Mtime { get; }

        /// <summary>
        ///   The optional modification time fractional nanoseconds (UnixFS 1.5).
        /// </summary>
        /// <remarks>
        ///   Corresponds to <c>UnixTime.FractionalNanoseconds</c> in the UnixFS protobuf schema.
        ///   Will be <c>null</c> if not set on the underlying DAG node, or if populated from a listing API
        ///   that does not decode the UnixFS <c>Data</c> blob.
        /// </remarks>
        int? MtimeNsecs { get; }
    }
}
