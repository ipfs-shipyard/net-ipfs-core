using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Ipfs.CoreApi
{
    /// <summary>
    ///   Manages the IPLD (linked data) Directed Acrylic Graph.
    /// </summary>
    /// <remarks>
    ///   The DAG API is a replacement for the object API, which only supported 'dag-pb'.
    ///   This API supports other IPLD formats, such as cbor, ethereum-block, git, ...
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/DAG.md">Dag API spec</seealso>
    public interface IDagApi
    {
        /// <summary>
        ///  Put JSON data as an IPLD node.
        /// </summary>
        /// <param name="data">
        ///   The JSON data to send to the network.
        /// </param>
        /// <param name="storeCodec">
        ///   The codec that the stored <paramref name="data"/> will be encoded with; such as "dag-pb" or "dag-cbor".
        ///   See <see cref="MultiCodec"/> for more details.  Defaults to "dag-cbor".
        /// </param>
        /// <param name="inputCodec">
        ///   The codec that the input <paramref name="data"/> will be encoded with; such as "dag-json".
        ///   See <see cref="MultiCodec"/> for more details.  Defaults to "dag-json".
        /// </param>
        /// <param name="pin">
        ///   If <b>true</b> the <paramref name="data"/> is pinned to local storage and will not be
        ///   garbage collected.  The default is <b>true</b>.
        /// </param>
        /// <param name="hash">
        ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="allowBigBlock">
        ///   Disable block size check and allow creation of blocks bigger than 1MiB. Default is <b>false</b>. Required: No.
        ///   <para/>
        ///   WARNING: such blocks won't be transferable over the standard bitswap.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous put operation. The task's value is
        ///   the data's <see cref="Cid"/>.
        /// </returns>
        Task<Cid> PutAsync(
            JObject data,
            string storeCodec = "dag-cbor",
            string inputCodec = "dag-json",
            bool? pin = null,
            MultiHash? hash = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default);

        /// <summary>
        ///  Put a stream of JSON as an IPLD node.
        /// </summary>
        /// <param name="data">
        ///   The stream of JSON.
        /// </param>
        /// <param name="storeCodec">
        ///   The content type or format of the <paramref name="data"/>; such as "dag-pb" or "dag-cbor".
        ///   See <see cref="MultiCodec"/> for more details.  Defaults to "dag-cbor".
        /// </param>
        /// <param name="inputCodec">
        ///   The codec that the input <paramref name="data"/> will be encoded with; such as "dag-json".
        ///   See <see cref="MultiCodec"/> for more details.  Defaults to "dag-json".
        /// </param>
        /// <param name="pin">
        ///   If <b>true</b> the <paramref name="data"/> is pinned to local storage and will not be
        ///   garbage collected.  The default is <b>true</b>.
        /// </param>
        /// <param name="hash">
        ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="allowBigBlock">
        ///   Disable block size check and allow creation of blocks bigger than 1MiB. Default is <b>false</b>. Required: No.
        ///   <para/>
        ///   WARNING: such blocks won't be transferable over the standard bitswap.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous put operation. The task's value is
        ///   the data's <see cref="Cid"/>.
        /// </returns>
        Task<Cid> PutAsync(
            Stream data,
            string storeCodec = "dag-cbor",
            string inputCodec = "dag-json",
            bool? pin = null,
            MultiHash? hash = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default);

        /// <summary>
        ///  Put an object as an IPLD node.
        /// </summary>
        /// <param name="data">
        ///   The object to add.
        /// </param>
        /// <param name="storeCodec">
        ///   The content type or format of the <paramref name="data"/>; such as "dag-pb" or "dag-cbor".
        ///   See <see cref="MultiCodec"/> for more details.  Defaults to "dag-cbor".
        /// </param>
        /// <param name="inputCodec">
        ///   The codec that the input <paramref name="data"/> will be encoded with; such as "dag-json".
        ///   See <see cref="MultiCodec"/> for more details.  Defaults to "dag-json".
        /// </param>
        /// <param name="pin">
        ///   If <b>true</b> the <paramref name="data"/> is pinned to local storage and will not be
        ///   garbage collected.  The default is <b>true</b>.
        /// </param>
        /// <param name="hash">
        ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="allowBigBlock">
        ///   Disable block size check and allow creation of blocks bigger than 1MiB. Default is <b>false</b>. Required: No.
        ///   <para/>
        ///   WARNING: such blocks won't be transferable over the standard bitswap.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous put operation. The task's value is
        ///   the data's <see cref="Cid"/>.
        /// </returns>
        Task<Cid> PutAsync(
            object data,
            string storeCodec = "dag-cbor",
            string inputCodec = "dag-json",
            bool? pin = null,
            MultiHash? hash = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default);

        /// <summary>
        ///   Get an IPLD node.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the IPLD node.
        /// </param>
        /// <param name="outputCodec">
        /// Format that the object will be encoded as. Default: dag-json. Required: no.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   contains the node's content as JSON.
        /// </returns>
        Task<JObject> GetAsync(Cid id, string outputCodec = "dag-json", CancellationToken cancel = default);

        /// <summary>
        ///   Gets the content of an IPLD node.
        /// </summary>
        /// <param name="path">
        ///   A path, such as "cid", "/ipfs/cid/" or "cid/a".
        /// </param>
        /// <param name="outputCodec">
        /// Format that the object will be encoded as. Default: dag-json. Required: no.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   contains the path's value.
        /// </returns>
        Task<JToken> GetAsync(string path, string outputCodec = "dag-json", CancellationToken cancel = default);

        /// <summary>
        ///   Get an IPLD node of the specific type.
        /// </summary>
        /// <typeparam name="T">
        ///   The object's type.
        /// </typeparam>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the IPLD node.
        /// </param>
        /// <param name="outputCodec">
        /// Format that the object will be encoded as. Default: dag-json. Required: no.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   is a new instance of the <typeparamref name="T"/> class, or null if the returned information
        ///   could not be parsed as type T.
        /// </returns>
        Task<T> GetAsync<T>(Cid id, string outputCodec = "dag-json", CancellationToken cancel = default);

        /// <summary>
        /// Resolve IPLD block.
        /// </summary>
        /// <param name="path">The path to resolve.</param>
        /// <param name="cancel">A token that can be used to cancel the ongoing operation.</param>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   is the resolved <see cref="DagResolveOutput"/>.
        /// </returns>
        Task<DagResolveOutput> ResolveAsync(string path, CancellationToken cancel = default);

        /// <summary>
        /// Yields stats for a DAG.
        /// </summary>
        /// <param name="cid">CID of a DAG root to get statistics for.</param>
        /// <param name="progress">If not null, progress updates will be sent to this object.</param>
        /// <param name="cancel">A token that can be used to cancel the ongoing operation.</param>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   is the <see cref="DagStatSummary"/>.
        /// </returns>
        Task<DagStatSummary> StatAsync(string cid, IProgress<DagStatSummary>? progress = null, CancellationToken cancel = default);

        /// <summary>
        /// Streams the provided DAG as a .car stream.
        /// </summary>
        /// <param name="cid">The CID of a root to recursively export Required: yes.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   is the <see cref="Stream"/> of the .car file.
        /// </returns>
        Task<Stream> ExportAsync(string cid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Import the contents of .car files
        /// </summary>
        /// <param name="stream">A stream containing the CAR file to import.</param>
        /// <param name="pinRoots">
        /// Pin optional roots listed in the .car headers after importing.
        /// Default: true. Required: no.
        /// </param>
        /// <param name="stats">Output stats. Required: no.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
        /// <returns></returns>
        Task<CarImportOutput> ImportAsync(Stream stream, bool? pinRoots = null, bool stats = false, CancellationToken cancellationToken = default);
    }
}
