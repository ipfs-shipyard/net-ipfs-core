using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   Provides access to a private network of peers.
    /// </summary>
    /// <remarks>
    ///   The <see cref="Swarm"/> calls the network protector whenever a connection
    ///   is being established with another peer.
    /// </remarks>
    /// <seealso href="https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md"/>
    public interface INetworkProtector
    {
        /// <summary>
        ///   Creates a protected stream for the connection.
        /// </summary>
        /// <param name="connection">
        ///   A connection between two peers.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the protected stream.
        /// </returns>
        /// <remarks>
        ///   <b>ProtectAsync</b> is called after the transport level has established
        ///   the connection.
        ///   <para>
        ///   An exception is thrown if the remote peer is not a member of
        ///   the private network.
        ///   </para>
        /// </remarks>
        Task<Stream> ProtectAsync(PeerConnection connection, CancellationToken cancel = default);
    }
}
