using System.Collections.Generic;

namespace Ipfs.CoreApi;


/// <summary>
///   A unified representation of a pin list entry that works for both
///   streaming and non-streaming list responses.
/// </summary>
public record PinListItem
{
    /// <summary>
    ///   The CID of the pinned object.
    /// </summary>
    public Ipfs.Cid Cid { get; init; } = null!;

    /// <summary>
    ///   The pin type (direct, indirect, recursive).
    /// </summary>
    public PinType Type { get; init; }

    /// <summary>
    ///   Optional pin name (present when names are requested and set).
    /// </summary>
    public string? Name { get; init; }
}


/// <summary>
///   Options for pin add.
/// </summary>
public record PinAddOptions
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
