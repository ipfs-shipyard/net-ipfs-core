using System;
using System.Collections.Generic;
using System.Text;
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
        public Task<IDupsResponse> DupsAsync();

        /// <summary>
        /// Lists objects in filestore.
        /// </summary>
        /// <returns></returns>
        public Task<IFilesStoreApiObjectResponse> ListAsync(string cid, bool fileOrder);

        /// <summary>
        /// Verifies objects in filestore.
        /// </summary>
        public Task<IFilesStoreApiObjectResponse> VerifyObjectsAsync(string cid, bool fileOrder);
    }
}
