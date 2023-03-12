using System;

namespace IpfsShipyard.Ipfs.Core.CoreApi;

/// <summary>
///   Reports the <see cref="IProgress{T}">progress</see> of
///   a transfer operation.
/// </summary>
public class TransferProgress
{
    /// <summary>
    ///   The name of the item being transferred.
    /// </summary>
    /// <value>
    ///   Typically, a relative file path.
    /// </value>
    public string Name;

    /// <summary>
    ///   The cumulative number of bytes transferred for
    ///   the <see cref="Name"/>.
    /// </summary>
    public ulong Bytes;
}