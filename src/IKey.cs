namespace Ipfs
{
    /// <summary>
    ///   Information about a cryptographic key.
    /// </summary>
    public interface IKey
    {
        /// <summary>
        ///   Unique identifier.
        /// </summary>
        /// <value>
        ///   A <see cref="Cid"/> containing the <see cref="MultiHash"/> of the public libp2p-key encoded in the requested Multibase.
        /// </value>
        /// <remarks>
        ///  The CID of the ipns libp2p-key encoded in the requested multibase.
        /// </remarks>
        Cid Id { get; }

        /// <summary>
        ///   The locally assigned name to the key.
        /// </summary>
        /// <value>
        ///   The name is only unique within the local peer node. The
        ///   <see cref="Id"/> is universally unique.
        /// </value>
        string Name { get; }
    }
}
