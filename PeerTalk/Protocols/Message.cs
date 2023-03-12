using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.PeerTalk.Protocols;

/// <summary>
///   A message that is exchanged between peers.
/// </summary>
/// <remarks>
///   A message consists of
///   <list type="bullet">
///      <item><description>A <see cref="Varint"/> length prefix</description></item>
///      <item><description>The payload</description></item>
///      <item><description>A terminating newline</description></item>
///   </list>
/// </remarks>
public static class Message
{
    private static readonly byte[] newline = "\n"u8.ToArray();
    private static readonly ILog log = LogManager.GetLogger(typeof(Message));

    /// <summary>
    ///   Read the message as a sequence of bytes from the <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">
    ///   The <see cref="Stream"/> to a peer.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task's result
    ///   is the byte representation of the message's payload.
    /// </returns>
    /// <exception cref="InvalidDataException">
    ///   When the message is invalid.
    /// </exception>
    public static async Task<byte[]> ReadBytesAsync(Stream stream, CancellationToken cancel = default)
    {
        var eol = new byte[1];
        var length = await stream.ReadVarint32Async(cancel).ConfigureAwait(false);
        var buffer = new byte[length - 1];
        await stream.ReadExactAsync(buffer, 0, length - 1, cancel).ConfigureAwait(false);
        await stream.ReadExactAsync(eol, 0, 1, cancel).ConfigureAwait(false);
        if (eol[0] != newline[0])
        {
            log.Error($"length: {length}, bytes: {buffer.ToHexString()}");
            throw new InvalidDataException("Missing terminating newline");
        }
        return buffer;
    }

    /// <summary>
    ///   Read the message as a <see cref="string"/> from the <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">
    ///   The <see cref="Stream"/> to a peer.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task's result
    ///   is the string representation of the message's payload.
    /// </returns>
    /// <exception cref="InvalidDataException">
    ///   When the message is invalid.
    /// </exception>
    /// <remarks>
    ///   The return value has the length prefix and terminating newline removed.
    /// </remarks>
    public static async Task<string> ReadStringAsync(Stream stream, CancellationToken cancel = default)
    {
        var payload = Encoding.UTF8.GetString(await ReadBytesAsync(stream, cancel).ConfigureAwait(false));

        log.Trace("received " + payload);
        return payload;
    }

    /// <summary>
    ///   Writes the binary representation of the message to the specified <see cref="Stream"/>.
    /// </summary>
    /// <param name="message">
    ///   The message to write.  A newline is automatically appended.
    /// </param>
    /// <param name="stream">
    ///   The <see cref="Stream"/> to a peer.
    /// </param>
    /// <param name="cancel">
    ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation.
    /// </returns>
    public static async Task WriteAsync(string message, Stream stream, CancellationToken cancel = default)
    {
        log.Trace("sending " + message);

        var payload = Encoding.UTF8.GetBytes(message);
        await stream.WriteVarintAsync(message.Length + 1, cancel).ConfigureAwait(false);
        await stream.WriteAsync(payload, 0, payload.Length, cancel).ConfigureAwait(false);
        await stream.WriteAsync(newline, 0, newline.Length, cancel).ConfigureAwait(false);
        await stream.FlushAsync(cancel).ConfigureAwait(false);
    }
}