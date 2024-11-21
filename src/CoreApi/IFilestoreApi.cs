using System.Collections.Generic;
using System.Threading;

namespace Ipfs.CoreApi
{
    /// <summary>
    /// Manages the filestore API.
    /// </summary>
    public interface IFilestoreApi
    {
        /// <summary>
        /// Lists blocks that are both in the filestore and standard block storage.
        /// </summary>
        public IAsyncEnumerable<FilestoreDuplicate> DupsAsync(CancellationToken token = default);

        /// <summary>
        /// Lists filestore objects
        /// </summary>
        /// <param name="cid">Cid of objects to verify. Required: no.</param>
        /// <param name="fileOrder">Sort the results based on the path of the backing file. Required: no.</param>
        /// <param name="token">A token that can be used to cancel the ongoing operation.</param>
        public IAsyncEnumerable<FilestoreItem> ListAsync(string? cid = null, bool? fileOrder = null, CancellationToken token = default);

        /// <summary>
        /// Verify objects in filestore.
        /// </summary>
        /// <param name="cid">Cid of objects to list. Required: no.</param>
        /// <param name="fileOrder">Sort the results based on the path of the backing file. Required: no.</param>
        /// <param name="token">A token that can be used to cancel the ongoing operation.</param>
        public IAsyncEnumerable<FilestoreItem> VerifyObjectsAsync(string? cid = null, bool? fileOrder = null, CancellationToken token = default);
    }
}
