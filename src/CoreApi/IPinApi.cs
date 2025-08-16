using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System;

namespace Ipfs.CoreApi
{
    /// <summary>
    ///   Manage pinned objects (locally stored and permanent).
    /// </summary>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/PIN.md">Pin API spec</seealso>
    public interface IPinApi
    {
        /// <summary>
        ///   Adds an IPFS object to the pinset and also stores it to the IPFS repo. pinset is the set of hashes currently pinned (not gc'able).
        /// </summary>
        /// <param name="path">
        ///   A CID or path to an existing object, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="options">
        ///   Options for pinning (name and recursion). If null, defaults are used.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value
        ///   is a sequence of <see cref="Cid"/> that were pinned.
        /// </returns>
        Task<IEnumerable<Cid>> AddAsync(string path, PinAddOptions options, CancellationToken cancel = default);

        /// <summary>
        ///   Adds an IPFS object to the pinset with progress reporting.
        /// </summary>
        /// <param name="path">The CID or path of the object to pin.</param>
        /// <param name="options">Options for pinning (name and recursion).</param>
        /// <param name="progress">Receives cumulative blocks-pinned updates.</param>
        /// <param name="cancel">Cancellation token.</param>
        /// <returns>Pinned CIDs on completion.</returns>
        Task<IEnumerable<Cid>> AddAsync(string path, PinAddOptions options, IProgress<BlocksPinnedProgress> progress, CancellationToken cancel = default);

        /// <summary>
        ///   List all the objects pinned to local storage.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>An async sequence of <see cref="PinListItem"/>.</returns>
        IAsyncEnumerable<PinListItem> ListAsync(CancellationToken cancel = default);

        /// <summary>
        ///   List all the objects pinned to local storage.
        /// </summary>
        /// <param name="type">
        ///   The type of pin to list.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>An async sequence of <see cref="PinListItem"/>.</returns>
        IAsyncEnumerable<PinListItem> ListAsync(PinType type, CancellationToken cancel = default);

        /// <summary>
        ///   List pinned objects with advanced options.
        /// </summary>
        /// <param name="options">List options (type filter, names, stream, etc.).</param>
        /// <param name="cancel">Cancellation token.</param>
        /// <returns>An async sequence of <see cref="PinListItem"/>.</returns>
        IAsyncEnumerable<PinListItem> ListAsync(PinListOptions options, CancellationToken cancel = default);

        /// <summary>
        ///   Unpin an object.
        /// </summary>
        /// <param name="id">
        ///   The CID of the object.
        /// </param>
        /// <param name="recursive">
        ///   <b>true</b> to recursively unpin links of object; otherwise, <b>false</b> to only unpin
        ///   the specified object.  Default is <b>true</b>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value
        ///   is a sequence of <see cref="Cid"/> that were unpinned.
        /// </returns>
        Task<IEnumerable<Cid>> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default);


    }
}
