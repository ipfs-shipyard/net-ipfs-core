namespace IpfsShipyard.Ipfs.Core.CoreApi;

/// <summary>
///   The statistics for <see cref="IStatsApi.BandwidthAsync"/>.
/// </summary>
public class BandwidthData
{
    /// <summary>
    ///   The number of bytes received.
    /// </summary>
    public ulong TotalIn;

    /// <summary>
    ///   The number of bytes sent.
    /// </summary>
    public ulong TotalOut;

    /// <summary>
    ///   TODO
    /// </summary>
    public double RateIn;

    /// <summary>
    ///   TODO
    /// </summary>
    public double RateOut;

}