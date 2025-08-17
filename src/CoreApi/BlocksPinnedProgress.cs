namespace Ipfs.CoreApi;

/// <summary>
///   Progress notification for pin/add reporting cumulative blocks pinned.
/// </summary>
public record BlocksPinnedProgress
{
    /// <summary>
    ///   The cumulative number of blocks pinned so far.
    /// </summary>
    public int BlocksPinned { get; init; }
}
