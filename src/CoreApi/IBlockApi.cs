using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Ipfs.CoreApi
{
    /// <summary>
    ///   Manages IPFS blocks.
    /// </summary>
    /// <remarks>
    ///   An IPFS Block is a byte sequence that represents an IPFS Object 
    ///   (i.e. serialized byte buffers). It is useful to talk about them as 
    ///   "blocks" in <see cref="IBitswapApi">Bitswap</see>
    ///   and other things that do not care about what is being stored. 
    /// </remarks>
    /// <seealso cref="IBlockRepositoryApi"/>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/BLOCK.md">Block API spec</seealso>
    public interface IBlockApi
    {
        /// <summary>
        ///   Gets an IPFS block.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the block.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous get operation. The task's value
        ///    contains the block's id and data.
        /// </returns>
        Task<byte[]> GetAsync(Cid id, CancellationToken cancel = default);

        /// <summary>
        ///   Stores a byte array as an IPFS block.
        /// </summary>
        /// <param name="data">
        ///   The byte array to send to the IPFS network.
        /// </param>
        /// <param name="cidCodec">
        ///   Multicodec to use in returned CID.
        ///   Default: raw. Required: no.
        /// </param>
        /// <param name="hash">
        ///   The <see cref="MultiHash"/> algorithm used to produce the <see cref="Cid"/>.
        ///   Required: no.
        /// </param>
        /// <param name="pin">
        ///   Pin added blocks recursively.
        ///   Default: false. Required: no.
        /// </param>
        /// <param name="allowBigBlock">
        /// Disable block size check and allow creation of blocks bigger than 1MiB.
        /// WARNING: such blocks won't be transferable over the standard bitswap.
        /// Default: false. Required: no.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous put operation. The task's value is
        ///    the block's <see cref="Cid"/>.
        /// </returns>
        public Task<IBlockStat> PutAsync(
            byte[] data,
            string cidCodec = "raw",
            MultiHash? hash = null,
            bool? pin = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default);

        /// <summary>
        ///   Stores a stream as an IPFS block.
        /// </summary>
        /// <param name="data">
        ///   The <see cref="Stream"/> of data to send to the IPFS network.
        /// </param>
        /// <param name="cidCodec">
        ///   Multicodec to use in returned CID.
        ///   Default: raw. Required: no.
        /// </param>
        /// <param name="hash">
        ///   The <see cref="MultiHash"/> algorithm used to produce the <see cref="Cid"/>.
        ///   Required: no.
        /// </param>
        /// <param name="pin">
        ///   Pin added blocks recursively.
        ///   Default: false. Required: no.
        /// </param>
        /// <param name="allowBigBlock">
        /// Disable block size check and allow creation of blocks bigger than 1MiB.
        /// WARNING: such blocks won't be transferable over the standard bitswap.
        /// Default: false. Required: no.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous put operation. The task's value is
        ///    the block's <see cref="Cid"/>.
        /// </returns>
        public Task<IBlockStat> PutAsync(
            Stream data,
            string cidCodec = "raw",
            MultiHash? hash = null,
            bool? pin = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default);

        /// <summary>
        ///   Information on an IPFS block.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the block.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous operation. The task's value
        ///    contains the block's id and size or <b>null</b>.
        /// </returns>
        /// <remarks>
        ///   Only the local repository is consulted for the block.  If <paramref name="id"/>
        ///   does not exist, then <b>null</b> is retuned.
        /// </remarks>
        Task<IBlockStat> StatAsync(Cid id, CancellationToken cancel = default);

        /// <summary>
        ///   Remove an IPFS block.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the block.
        /// </param>
        /// <param name="ignoreNonexistent">
        ///   If <b>true</b> do not raise exception when <paramref name="id"/> does not
        ///   exist.  Default value is <b>false</b>.
        /// </param>
        /// <returns>
        ///   The awaited Task will return the deleted <paramref name="id"/> or <b>null</b>
        ///   if the <paramref name="id"/> does not exist and <paramref name="ignoreNonexistent"/>
        ///   is <b>true</b>.
        /// </returns>
        /// <remarks>
        ///   This removes the block from the local cache and does not affect other peers.
        /// </remarks>
        Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default);
    }
}
