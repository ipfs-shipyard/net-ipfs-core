using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;

namespace PeerTalk
{
    /// <summary>
    ///   Helper methods for ProtoBuf.
    /// </summary>
    public static class ProtoBufHelper
    {
        /// <summary>
        ///   Read a proto buf message with a varint length prefix.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of message.
        /// </typeparam>
        /// <param name="stream">
        ///   The stream containing the message.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the <typeparamref name="T"/> message.
        /// </returns>
        public static async Task<T> ReadMessageAsync<T>(Stream stream, CancellationToken cancel = default)
        {
            var length = await stream.ReadVarint32Async(cancel).ConfigureAwait(false);
            var bytes = new byte[length];
            await stream.ReadExactAsync(bytes, 0, length, cancel).ConfigureAwait(false);

            using (var ms = new MemoryStream(bytes, false))
            {
                return ProtoBuf.Serializer.Deserialize<T>(ms);
            }
        }
    }
}
