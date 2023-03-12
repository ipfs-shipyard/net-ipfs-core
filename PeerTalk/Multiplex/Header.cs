using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.PeerTalk.Multiplex
{
    /// <summary>
    ///   The header of a multiplex message.
    /// </summary>
    /// <remarks>
    ///   The header of a multiplex message contains the <see cref="StreamId"/> and
    ///   <see cref="PacketType"/> encoded as a <see cref="Varint">variable integer</see>.
    /// </remarks>
    /// <seealso href="https://github.com/libp2p/mplex"/>
    public struct Header
    {
        /// <summary>
        ///   The largest possible value of a <see cref="StreamId"/>.
        /// </summary>
        /// <value>
        ///   long.MaxValue >> 3.
        /// </value>
        public const long MaxStreamId = long.MaxValue >> 3;

        /// <summary>
        ///   The smallest possible value of a <see cref="StreamId"/>.
        /// </summary>
        /// <value>
        ///   Zero.
        /// </value>
        public const long MinStreamId = 0;

        /// <summary>
        ///   The stream identifier.
        /// </summary>
        /// <value>
        ///   The session initiator allocates odd IDs and the session receiver allocates even IDs.
        /// </value>
        public long StreamId;

        /// <summary>
        ///   The purpose of the multiplex message.
        /// </summary>
        /// <value>
        ///   One of the <see cref="PacketType"/> enumeration values.
        /// </value>
        public PacketType PacketType;

        /// <summary>
        ///   Writes the header to the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The destination <see cref="Stream"/> for the header.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public async Task WriteAsync(Stream stream, CancellationToken cancel = default)
        {
            var header = (StreamId << 3) | (long)PacketType;
            await stream.WriteVarintAsync(header, cancel).ConfigureAwait(false);
        }

        /// <summary>
        ///   Reads the header from the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The source <see cref="Stream"/> for the header.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.  The task's result
        ///   is the decoded <see cref="Header"/>.
        /// </returns>
        public static async Task<Header> ReadAsync(Stream stream, CancellationToken cancel = default)
        {
            var varint = await stream.ReadVarint64Async(cancel).ConfigureAwait(false);
            return new Header
            {
                StreamId = varint >> 3,
                PacketType = (PacketType)((byte)varint & 0x7)
            };
        }
    }
}