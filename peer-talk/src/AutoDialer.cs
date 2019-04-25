using Common.Logging;
using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   Maintains a minimum number of peer connections.
    /// </summary>
    /// <remarks>
    ///   Listens to the <see cref="Swarm"/> and automically dials a
    ///   new <see cref="Peer"/> when required.
    /// </remarks>
    public class AutoDialer : IDisposable
    {
        static readonly ILog log = LogManager.GetLogger(typeof(AutoDialer));

        /// <summary>
        ///   The default minimum number of connections to maintain (16).
        /// </summary>
        public const int DefaultMinConnections = 16;

        readonly Swarm swarm;
        int pendingConnects;

        /// <summary>
        ///   Creates a new instance of the <see cref="AutoDialer"/> class.
        /// </summary>
        /// <param name="swarm">
        ///   Provides access to other peers.
        /// </param>
        public AutoDialer(Swarm swarm)
        {
            this.swarm = swarm;
            swarm.PeerDiscovered += OnPeerDiscovered;
            swarm.PeerDisconnected += OnPeerDisconnected;
        }

        /// <summary>
        ///  Releases the unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <b>true</b> to release both managed and unmanaged resources; <b>false</b> 
        ///   to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                swarm.PeerDiscovered -= OnPeerDiscovered;
                swarm.PeerDisconnected -= OnPeerDisconnected;
            }
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, 
        ///   releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        
        /// <summary>
        ///   The low water mark for peer connections.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="DefaultMinConnections"/>.
        /// </value>
        /// <remarks>
        ///   Setting this to zero will basically disable the auto dial features.
        /// </remarks>
        public int MinConnections { get; set; } = DefaultMinConnections;

        /// <summary>
        ///   Called when the swarm has a new peer.
        /// </summary>
        /// <param name="sender">
        ///   The swarm of peers.
        /// </param>
        /// <param name="peer">
        ///   The peer that was discovered.
        /// </param>
        /// <remarks>
        ///   If the <see cref="MinConnections"/> is not reached, then the
        ///   <paramref name="peer"/> is dialed.
        /// </remarks>
        void OnPeerDiscovered(object sender, Peer peer)
        {
            var n = swarm.Manager.Connections.Count() + pendingConnects;
            if (swarm.IsRunning && n < MinConnections)
            {
                Interlocked.Increment(ref pendingConnects);
                Task.Run(async () =>
                {
                    log.Debug($"Dialing new {peer}");
                    try
                    {
                        await swarm.ConnectAsync(peer);
                    }
                    catch(Exception e)
                    {
                        log.Warn($"Failed to dial {peer}", e);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref pendingConnects);
                    }
                });
            }
        }

        /// <summary>
        ///   Called when the swarm has lost a connection to a peer.
        /// </summary>
        /// <param name="sender">
        ///   The swarm of peers.
        /// </param>
        /// <param name="disconnectedPeer">
        ///   The peer that was disconnected.
        /// </param>
        /// <remarks>
        ///   If the <see cref="MinConnections"/> is not reached, then another
        ///   peer is dialed.
        /// </remarks>
        void OnPeerDisconnected(object sender, Peer disconnectedPeer)
        {
            var n = swarm.Manager.Connections.Count() + pendingConnects;
            if (!swarm.IsRunning || n >= MinConnections)
                return;

            // Find a peer to connect to.
            var peer = swarm.KnownPeers
                .Where(p => p.ConnectedAddress == null)
                .Where(p => p != disconnectedPeer)
                .FirstOrDefault();
            if (peer != null)
            {
                Interlocked.Increment(ref pendingConnects);
                Task.Run(async () =>
                {
                    log.Debug($"Dialing {peer}");
                    try
                    {
                        await swarm.ConnectAsync(peer);
                    }
                    catch (Exception e)
                    {
                        log.Warn($"Failed to dial {peer}", e);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref pendingConnects);
                    }
                });
            }
        }

    }
}
