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
