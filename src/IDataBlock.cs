namespace Ipfs
{
    /// <summary>
    ///   Some data that is stored in IPFS.
    /// </summary>
    /// <remarks>
    ///   A <b>DataBlock</b> has a unique <see cref="Id">.</see>
    ///   and some data.
    ///   <para>
    ///   It is useful to talk about them as "blocks" in Bitswap 
    ///   and other things that do not care about what is being stored.
    ///   </para>
    /// </remarks>
    /// <seealso cref="IMerkleNode{Link}"/>
    public interface IDataBlock
    {
        /// <summary>
        ///   The unique ID of the data.
        /// </summary>
        /// <value>
        ///   A <see cref="Cid"/> of the content.
        /// </value>
        Cid Id { get; }

        /// <summary>
        ///   The size (in bytes) of the data.
        /// </summary>
        /// <value>Number of bytes.</value>
        long Size { get; }
    }
}
