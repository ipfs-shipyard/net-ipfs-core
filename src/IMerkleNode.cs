using System.Collections.Generic;

namespace Ipfs
{
    /// <summary>
    ///   A Directed Acyclic Graph (DAG) in IPFS.
    /// </summary>
    /// <remarks>
    ///   A <b>MerkleNode</b> has a sequence of navigable <see cref="Links"/>
    ///   and some data.
    /// </remarks>
    /// <typeparam name="Link">
    ///   The type of <see cref="IMerkleLink"/> used by this node.
    /// </typeparam>
    /// <seealso href="https://en.wikipedia.org/wiki/Directed_acyclic_graph"/>
    /// <seealso href="https://github.com/ipfs/specs/tree/master/merkledag"/>
    public interface IMerkleNode<out Link> : IDataBlock
        where Link : IMerkleLink
    {
        /// <summary>
        ///   Links to other nodes.
        /// </summary>
        /// <value>
        ///   A sequence of <typeparamref name="Link"/>.
        /// </value>
        /// <remarks>
        ///   It is never <b>null</b>.
        ///   <para>
        ///   The links are sorted ascending by <see cref="IMerkleLink.Name"/>. A <b>null</b>
        ///   name is compared as "".
        ///   </para>
        /// </remarks>
        IEnumerable<Link> Links { get; }

        /// <summary>
        ///   Returns a link to the node.
        /// </summary>
        /// <param name="name">
        ///   A <see cref="IMerkleLink.Name"/> for the link; defaults to "".
        /// </param>
        /// <returns>
        ///   A new <see cref="IMerkleLink"/> to the node.
        /// </returns>
        Link ToLink(string name = "");

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
