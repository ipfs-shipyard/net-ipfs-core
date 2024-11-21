namespace Ipfs
{
    /// <summary>
    ///   A link to another node in IPFS.
    /// </summary>
    public interface IMerkleLink
    {
        /// <summary>
        ///   A name associated with the linked node.
        /// </summary>
        /// <value>A <see cref="string"/> or <b>null</b>.</value>
        /// <remarks>
        ///   <note type="warning">
        ///   IPFS considers a <b>null</b> name different from a <see cref="string.Empty"/>
        ///   name;
        ///   </note>
        /// </remarks>
        string? Name { get; }

        /// <summary>
        ///   The unique ID of the link.
        /// </summary>
        /// <value>
        ///   A <see cref="Cid"/> of the content.
        /// </value>
        Cid Id { get; }

        /// <summary>
        ///   The serialised size (in bytes) of the linked node.
        /// </summary>
        /// <value>Number of bytes.</value>
        /// <remarks>
        /// Both <see cref="IMerkleLink"/> and <see cref="IMerkleNode{Link}"/> have a <see cref="Size"/> of type <see cref="ulong"/>.
        /// <para/>
        /// See <see href="https://github.com/ipfs/boxo/blob/main/ipld/merkledag/pb/merkledag.pb.go#L654-L697"/>.
        /// </remarks>
        ulong Size { get; }
    }
}
