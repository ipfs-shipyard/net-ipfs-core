using System.Collections.Generic;

namespace IpfsShipyard.Ipfs.Core;

/// <summary>
///   A Directed Acyclic Graph (DAG) in IPFS.
/// </summary>
/// <remarks>
///   A <b>MerkleNode</b> has a sequence of navigable <see cref="Links"/>
///   and some data (<see cref="IDataBlock.DataBytes"/> 
///   or <see cref="IDataBlock.DataStream"/>).
/// </remarks>
/// <typeparam name="TLink">
///   The type of <see cref="IMerkleLink"/> used by this node.
/// </typeparam>
/// <seealso href="https://en.wikipedia.org/wiki/Directed_acyclic_graph"/>
/// <seealso href="https://github.com/ipfs/specs/tree/master/merkledag"/>
public interface IMerkleNode<out TLink> : IDataBlock
    where TLink : IMerkleLink
{

    /// <summary>
    ///   Links to other nodes.
    /// </summary>
    /// <value>
    ///   A sequence of <typeparamref name="TLink"/>.
    /// </value>
    /// <remarks>
    ///   It is never <b>null</b>.
    ///   <para>
    ///   The links are sorted ascending by <see cref="IMerkleLink.Name"/>. A <b>null</b>
    ///   name is compared as "".
    ///   </para>
    /// </remarks>
    IEnumerable<TLink> Links { get; }

    /// <summary>
    ///   Returns a link to the node.
    /// </summary>
    /// <param name="name">
    ///   A <see cref="IMerkleLink.Name"/> for the link; defaults to "".
    /// </param>
    /// <returns>
    ///   A new <see cref="IMerkleLink"/> to the node.
    /// </returns>
    TLink ToLink(string name = "");

}