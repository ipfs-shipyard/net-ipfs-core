﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.CoreApi
{
    /// <summary>
    ///   Manages the files/directories in IPFS.
    /// </summary>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/FILES.md">Files API spec</seealso>
    public interface IFileSystemApi
    {
        /// <summary>
        ///   Add a local file to the interplanetary file system.
        /// </summary>
        /// <param name="path">
        ///   The name of the local file.
        /// </param>
        /// <param name="options">
        ///   The options when adding data to the IPFS file system.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous operation. The task's value is
        ///    the file's node.
        /// </returns>
        Task<IFileSystemNode> AddFileAsync(string path, AddFileOptions? options = default, CancellationToken cancel = default);

        /// <summary>
        ///   Add some text to the interplanetary file system.
        /// </summary>
        /// <remarks>
        /// See <see href="https://bafybeifacbihy4k7klhsw4ho3sk7osymct3v3zttp5knhe7e3qnyyv6d34.ipfs.inbrowser.link/reference/kubo/rpc/#api-v0-add"/>.
        /// </remarks>
        /// <param name="text">
        ///   The string to add to IPFS.  It is UTF-8 encoded.
        /// </param>
        /// <param name="options">
        ///   The options when adding data to the IPFS file system.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the text's node.
        /// </returns>
        Task<IFileSystemNode> AddTextAsync(string text, AddFileOptions? options = default, CancellationToken cancel = default);

        /// <summary>
        /// Adds the specified file parts and folder parts to the interplanetary file system.
        /// </summary>
        /// <param name="fileParts">The file parts to add. To specify a destination folder, use a relative path here.</param>
        /// <param name="folderParts">The folders to create.</param>
        /// <param name="options">The options when adding data to the IPFS file system.</param>
        /// <param name="cancel">A token that can be used to cancel the ongoing operation.</param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the data's node.
        /// </returns>
        IAsyncEnumerable<IFileSystemNode> AddAsync(FilePart[] fileParts, FolderPart[] folderParts, AddFileOptions? options = default, CancellationToken cancel = default);

        /// <summary>
        ///   Add a <see cref="Stream"/> to interplanetary file system.
        /// </summary>
        /// <remarks>
        /// See <see href="https://bafybeifacbihy4k7klhsw4ho3sk7osymct3v3zttp5knhe7e3qnyyv6d34.ipfs.inbrowser.link/reference/kubo/rpc/#api-v0-add"/>.
        /// </remarks>
        /// <param name="stream">
        ///   The stream of data to add to IPFS.
        /// </param>
        /// <param name="name">
        ///   A name for the <paramref name="stream"/>.
        /// </param>
        /// <param name="options">
        ///   The options when adding data to the IPFS file system.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the data's node.
        /// </returns>
        Task<IFileSystemNode> AddAsync(Stream stream, string name = "", AddFileOptions? options = default, CancellationToken cancel = default);

        /// <summary>
        ///   Reads the content of an existing IPFS file as text.
        /// </summary>
        /// <param name="path">
        ///   A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the contents of the <paramref name="path"/> as a <see cref="string"/>.
        /// </returns>
        Task<string> ReadAllTextAsync(string path, CancellationToken cancel = default);

        /// <summary>
        ///   Reads an existing IPFS file.
        /// </summary>
        /// <param name="path">
        ///   An IPFS path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   a <see cref="Stream"/> to the file contents.
        /// </returns>
        /// <remarks>
        ///   The returned <see cref="Stream"/> must be disposed.
        /// </remarks>
        Task<Stream> ReadFileAsync(string path, CancellationToken cancel = default);

        /// <summary>
        ///   Reads an existing IPFS file with the specified offset and length.
        /// </summary>
        /// <param name="path">
        ///   Am IPFS path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="offset">
        ///   The position to start reading from.
        /// </param>
        /// <param name="count">
        ///   The number of bytes to read.  If zero, then the remaining bytes
        ///   from <paramref name="offset"/> are read.  Defaults to zero.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   a <see cref="Stream"/> to the file contents.
        /// </returns>
        /// <remarks>
        ///   The returned <see cref="Stream"/> must be disposed.
        /// </remarks>
        Task<Stream> ReadFileAsync(string path, long offset, long count = 0, CancellationToken cancel = default);

        /// <summary>
        ///   Get information about the file or directory.
        /// </summary>
        /// <param name="path">
        ///   A path to an existing file or directory, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task<IFileSystemNode> ListAsync(string path, CancellationToken cancel = default);

        /// <summary>
        ///   Download IPFS objects as a TAR archive.
        /// </summary>
        /// <param name="path">
        ///   An IPFS path to an existing file or directory, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="compress">
        ///   If <b>true</b>, the returned stream is compressed with the GZIP algorithm.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   a <see cref="Stream"/> containing a TAR archive.
        /// </returns>
        /// <remarks>
        ///   The returned TAR <see cref="Stream"/> must be disposed.
        ///   <para>
        ///   If the <paramref name="path"/> is a directory, then all files and all
        ///   sub-directories are returned; e.g. it is recursive.
        ///   </para>
        /// </remarks>
        Task<Stream> GetAsync(string path, bool compress = false, CancellationToken cancel = default);
    }
}
