namespace Ipfs.CoreApi
{
    /// <summary>
    ///   The type of pin.
    /// </summary>
    /// <remarks>
    ///   See <see href="https://docs.ipfs.tech/reference/kubo/rpc/#api-v0-pin-ls">type</see>
    ///   for more information.
    /// </remarks>
    public enum PinType
    {
        /// <summary>
        ///   Direct pin.
        /// </summary>
        Direct,
        /// <summary>
        ///   Indirect pin.
        /// </summary>
        Indirect,
        /// <summary>
        ///   Recursive pin.
        /// </summary>
        Recursive,
        /// <summary>
        ///   All pins.
        /// </summary>
        All
    }
} 