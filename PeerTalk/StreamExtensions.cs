using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    /// Stream Extensions
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        ///   Asynchronously reads a sequence of bytes from the stream and advances
        ///   the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="stream">
        ///   The stream to read from.
        /// </param>
        /// <param name="buffer">
        ///   The buffer to write the data into.
        /// </param>
        /// <param name="offset">
        ///   The byte offset in <paramref name="buffer"/> at which to begin
        ///   writing data from the <paramref name="stream"/>.
        /// </param>
        /// <param name="length">
        ///  The number of bytes to read.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="EndOfStreamException">
        ///   When the <paramref name="stream"/> does not have
        ///   <paramref name="length"/> bytes.
        /// </exception>
        public static async Task ReadExactAsync(this Stream stream, byte[] buffer, int offset, int length)
        {
            while (0 < length)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(offset, length));
                if (n == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += n;
                length -= n;
            }
        }

        /// <summary>
        ///   Asynchronously reads a sequence of bytes from the stream and advances
        ///   the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="stream">
        ///   The stream to read from.
        /// </param>
        /// <param name="buffer">
        ///   The buffer to write the data into.
        /// </param>
        /// <param name="offset">
        ///   The byte offset in <paramref name="buffer"/> at which to begin
        ///   writing data from the <paramref name="stream"/>.
        /// </param>
        /// <param name="length">
        ///  The number of bytes to read.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="EndOfStreamException">
        ///   When the <paramref name="stream"/> does not have
        ///   <paramref name="length"/> bytes.
        /// </exception>
        public static async Task ReadExactAsync(this Stream stream, byte[] buffer, int offset, int length, CancellationToken cancel)
        {
            while (0 < length)
            {
                //var n = await stream.ReadAsync(buffer.AsMemory(offset, length), cancel);
                var n = await stream.ReadAsync(buffer.AsMemory(offset, length), cancel);
                if (n == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += n;
                length -= n;
            }
        }
    }
}