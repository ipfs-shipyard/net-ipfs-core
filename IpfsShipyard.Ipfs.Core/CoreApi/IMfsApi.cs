using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IpfsShipyard.Ipfs.Core.CoreApi;

/// <summary>
///   Manages the files/directories in MPS.
/// </summary>
/// <seealso href="https://docs.ipfs.tech/reference/kubo/rpc/#api-v0-files-chcid">Files API docs</seealso>
/// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/FILES.md">Files API spec</seealso>
public interface IMfsApi
{
    /// <summary>
    ///   Add references to IPFS files and directories in MFS (or copy within MFS).
    /// </summary>
    /// <param name="sourceMfsPathOrCid">
    ///   Source IPFS or MFS path to copy. Required: yes
    /// </param>
    /// <param name="destMfsPath">
    ///   Destination within MFS. Required: yes
    /// </param>
    /// <param name="parents">
    ///   Make parent directories as needed. Required: no
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    Task CopyAsync(string sourceMfsPathOrCid, string destMfsPath, bool? parents = null, CancellationToken cancel = default);

    /// <summary>
    ///   Flush a given path's data to disk.
    /// </summary>
    /// <param name="path">
    ///   Path to flush. Default: '/'. Required: no
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   <see cref="Cid"/> of the flushed path.
    /// </returns>
    Task<Cid> FlushAsync(string path = null, CancellationToken cancel = default);

    /// <summary>
    ///   List directories in the local mutable namespace.
    /// </summary>
    /// <param name="path">Path to show listing for. Defaults to '/'. Required: no
    /// </param>
    /// <param name="u">Do not sort; list entries in directory order. Required: no
    /// </param>
    /// <param name="cancel">Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>Collection of <see cref="IFileSystemNode"/>
    /// </returns>
    /// <remarks>
    ///   Paramter long is ommitted and should always be passed as true in implementation.
    /// </remarks>
    Task<IEnumerable<IFileSystemNode>> ListAsync(string path, bool? u = null, CancellationToken cancel = default);

    /// <summary>
    ///   Make directories in the local mutable namespace.
    /// </summary>
    /// <param name="path">
    ///   Path to dir to make. Required: yes
    /// </param>
    /// <param name="parents">
    ///   No error if existing, make parent directories as needed. Required: no
    /// </param>
    /// <param name="cidVersion">
    ///   Cid version to use. (experimental). Required: no
    /// </param>
    /// <param name="multiHash">
    ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>
    ///   Will set Cid version to 1 if used. (experimental). Required: no
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    Task MakeDirectoryAsync(string path, bool? parents = null, int? cidVersion = null, string multiHash = null, CancellationToken cancel = default);

    /// <summary>
    ///    Move file or directory to another path in MFS.
    /// </summary>
    /// <param name="sourceMfsPath">
    ///   Source file to move. Required: yes
    /// </param>
    /// <param name="destMfsPath">
    ///   Destination path for file to be moved to. Required: yes
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    Task MoveAsync(string sourceMfsPath, string destMfsPath, CancellationToken cancel = default);

    /// <summary>
    ///   Read a file from MFS.
    /// </summary>
    /// <param name="path">
    ///   Path to file to be read. Required: yes
    /// </param>
    /// <param name="offset">
    ///   Byte offset to begin reading from. Required: no
    /// </param>
    /// <param name="count">
    ///   Maximum number of bytes to read. Required: no
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    Task<string> ReadFileAsync(string path, long? offset = null, long? count = null, CancellationToken cancel = default);

    /// <summary>
    ///   Remove a file or directory or from MFS.
    /// </summary>
    /// <param name="path">
    ///   File or directory to remove. Required: yes
    /// </param>
    /// <param name="recursive">
    ///   Recursively remove directories. Must be true to remove a directory. Required: no
    /// </param>
    /// <param name="force">
    ///   Forcibly remove target at path; implies recursive for directories. Required: no
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    Task RemoveAsync(string path, bool? recursive = null, bool? force = null, CancellationToken cancel = default);

    /// <summary>
    ///   Get file or directory status info.
    /// </summary>
    /// <param name="path">
    ///   Path to node to stat. Required: yes
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   <see cref="FileStatResult"/> information about the path.
    /// </returns>
    Task<FileStatResult> StatAsync(string path, CancellationToken cancel = default);

    /// <summary>
    ///   Get file or directory status info including information about locality of data.
    /// </summary>
    /// <param name="path">
    ///   Path to node to stat. Required: yes
    /// </param>
    /// <param name="withLocal">
    ///   Populate locality information in result.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   <see cref="FileStatWithLocalityResult"/> information about the path including 
    ///   additional information on locality if withLocal=true.
    /// </returns>
    Task<FileStatWithLocalityResult> StatAsync(string path, bool withLocal, CancellationToken cancel = default);

    /// <summary>
    ///   Append to (modify) a text file in MFS.
    /// </summary>
    /// <param name="path">
    ///   Path to write to. Required: yes
    /// </param>
    /// <param name="text">
    ///   A <see cref="string"/> containing the text to write.
    /// </param>
    /// <param name="options">
    ///   The options when adding data to the IPFS file system.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <seealso cref="MfsWriteOptions"/>
    Task WriteAsync(string path, string text, MfsWriteOptions options, CancellationToken cancel = default);

    /// <summary>
    ///   Append to (modify) a binary file in MFS.
    /// </summary>
    /// <param name="path">
    ///   Path to write to. Required: yes
    /// </param>
    /// <param name="data">
    ///   A <see cref="string"/> containing the text to write.
    /// </param>
    /// <param name="options">
    ///   The options when adding data to the IPFS file system.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <seealso cref="MfsWriteOptions"/>
    Task WriteAsync(string path, byte[] data, MfsWriteOptions options, CancellationToken cancel = default);

    /// <summary>
    ///   Append to (modify) a file in MFS.
    /// </summary>
    /// <param name="path">
    ///   Path to write to. Required: yes
    /// </param>
    /// <param name="data">
    ///   A <see cref="Stream"/> containing the data to upload.
    /// </param>
    /// <param name="options">
    ///   The options when adding data to the IPFS file system.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <seealso cref="MfsWriteOptions"/>
    Task WriteAsync(string path, Stream data, MfsWriteOptions options, CancellationToken cancel = default);
}