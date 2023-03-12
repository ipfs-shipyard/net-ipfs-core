using System;

namespace IpfsShipyard.Ipfs.Core.CoreApi;

/// <summary>
///   The options when adding data to the MFS file system.
/// </summary>
/// <seealso cref="IMfsApi.WriteAsync(string, string, MfsWriteOptions, System.Threading.CancellationToken)"/>
/// <seealso cref="IMfsApi.WriteAsync(string, System.IO.Stream, MfsWriteOptions, System.Threading.CancellationToken)"/>
public class MfsWriteOptions
{
    /// <summary>
    ///   Create the file if it does not exist
    /// </summary>
    /// <value>
    ///   The default is <b>null</b> and the server will use its default of false.
    /// </value>
    public bool? Create { get; set; } = null;

    /// <summary>
    ///   Make parent directories as needed.
    /// </summary>
    /// <value>
    ///   The default is <b>null</b> and the server will use its default of false.
    /// </value>
    public bool? Parents { get; set; } = null;

    /// <summary>
    ///   Byte offset to begin writing at.
    /// </summary>
    /// <value>
    ///   The default is <b>null</b> and the argument will be omitted.
    ///   If ommitted the offset is zero.
    /// </value>
    public long? Offset { get; set; } = null;

    /// <summary>
    ///   Maximum number of bytes to write.
    /// </summary>
    /// <value>
    ///   The default is <b>null</b> and the argument will be omitted.
    ///   If ommitted, all the data will be written.
    /// </value>
    public long? Count { get; set; } = null;

    /// <summary>
    ///    Cid version to use.
    /// </summary>
    /// <value>
    ///   The default is <b>null</b> and the server will use its default Cid Version.
    /// </value>
    /// <seealso cref="Cid.Version"/>
    public int? CidVersion { get; set; } = null;

    /// <summary>
    ///   Truncate the file to size zero before writing
    /// </summary>
    /// <value>
    ///   The default is <b>null</b> and the server will use its default of false.
    /// </value>
    public bool? Truncate { get; set; } = null;

    /// <summary>
    ///   Use raw blocks for newly created leaf nodes.
    /// </summary>
    /// <value>
    ///   The default is <b>null</b> and the server will use its default of false.
    /// </value>
    public bool? RawLeaves { get; set; } = null;

    /// <summary>
    ///   The hashing algorithm name to use.
    /// </summary>
    /// <value>
    ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
    ///   The default is <b>null</b> and the server will use its default algorithm name.
    /// </value>
    /// <seealso cref="MultiHash.DefaultAlgorithmName"/>
    public string Hash { get; set; } = null;
}