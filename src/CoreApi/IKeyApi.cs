﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.CoreApi
{
    /// <summary>
    ///   Manages cryptographic keys.
    /// </summary>
    /// <remarks>
    ///   <note>
    ///   The Key API is work in progress! There be dragons here.
    ///   </note>
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/KEY.md">Key API spec</seealso>
    public interface IKeyApi
    {
        /// <summary>
        ///   Creates a new key.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="keyType">
        ///   The type of key to create; "rsa" or "ed25519".
        /// </param>
        /// <param name="size">
        ///   The size, in bits, of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the key that was created.
        /// </returns>
        Task<IKey> CreateAsync(
            string name,
            string keyType,
            int size,
            CancellationToken cancel = default);

        /// <summary>
        ///   List all the keys.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a sequence of IPFS keys.
        /// </returns>
        Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default);

        /// <summary>
        ///   Delete the specified key.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the key that was deleted, or null if the key is not present.
        /// </returns>
        Task<IKey?> RemoveAsync(string name, CancellationToken cancel = default);

        /// <summary>
        ///   Rename the specified key.
        /// </summary>
        /// <param name="oldName">
        ///   The local name of the key.
        /// </param>
        /// <param name="newName">
        ///   The new local name of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the new key after the rename.
        /// </returns>
        Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default);

        /// <summary>
        ///   Export a key to a PEM encoded password protected PKCS #8 container.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="password">
        ///   The PEM's password.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous operation. The task's result is
        ///    the password protected PEM string.
        /// </returns>
        Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default);

        /// <summary>
        ///   Import a key from a PEM encoded password protected PKCS #8 container.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="pem">
        ///   The PEM encoded PKCS #8 container.
        /// </param>
        /// <param name="password">
        ///   The <paramref name="pem"/>'s password.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous operation. The task's result
        ///    is the newly imported key.
        /// </returns>
        Task<IKey> ImportAsync(string name, string pem, char[]? password = null, CancellationToken cancel = default);
    }
}
