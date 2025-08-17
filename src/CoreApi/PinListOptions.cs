namespace Ipfs.CoreApi;

/// <summary>
///   Options for pin list.
/// </summary>
public record PinListOptions
{
    /// <summary>
    ///   The type of pinned keys to list. Can be "direct", "indirect", "recursive", or "all".
    ///   Default is "all".
    /// </summary>
    public PinType Type { get; set; } = PinType.All;

    /// <summary>
    ///   Output only the CIDs of pins.
    /// </summary>
    public bool Quiet { get; set; }

    /// <summary>
    ///   Limit returned pins to ones with names that contain the value provided (case-sensitive, partial match).
    ///   Implies Names = true.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///   Enable streaming of pins as they are discovered.
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    ///   Include pin names in the output (slower, disabled by default).
    /// </summary>
    public bool Names { get; set; }
}
