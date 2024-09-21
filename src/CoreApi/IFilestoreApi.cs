using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.CoreApi
{
    /// <summary>
    /// Abstraction for the file store api.
    /// </summary>
    public interface IFilestoreApi
    {
        /// <summary>
        /// Lists blocks that are both in the filestore and standard block storage.
        /// </summary>
        public Task<IDupsResponse> DupsAsync(CancellationToken token);

        /// <summary>
        /// Lists filestore objects
        /// </summary>
        /// <param name="cid">Cid of objects to verify. Required: no.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="fileOrder">Verify the objects based on the order of the backing file. Required: no.</param>
        /// <returns></returns>
        public Task<IFilestoreApiObjectResponse> ListAsync(string cid, bool fileOrder, CancellationToken token);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cid">Cid of objects to list. Required: no.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="fileOrder">Lists the objects based on the order of the backing file. Required: no.</param>
        /// <returns></returns>
        public Task<IFilestoreApiObjectResponse> VerifyObjectsAsync(string cid, bool fileOrder, CancellationToken token);
    }
}
