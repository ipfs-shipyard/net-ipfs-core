using Common.Logging;
using ProtoBuf;
using Semver;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;

namespace PeerTalk.Protocols
{
    /// <summary>
    ///   Identifies the peer.
    /// </summary>
    public class Identify1 : IPeerProtocol
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Identify1));

        /// <inheritdoc />
        public string Name { get; } = "ipfs/id";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1, 0);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Name}/{Version}";
        }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection, Stream stream, CancellationToken cancel = default)
        {
            // Send our identity.
            log.Debug("Sending identity to " + connection.RemoteAddress);
            var peer = connection.LocalPeer;
            var res = new Identify
            {
                ProtocolVersion = peer.ProtocolVersion,
                AgentVersion = peer.AgentVersion,
                ListenAddresses = peer.Addresses
                     .Select(a => a.WithoutPeerId().ToArray())
                     .ToArray(),
                ObservedAddress = connection.RemoteAddress?.ToArray(),
                Protocols = null, // no longer sent
            };
            if (peer.PublicKey != null)
            {
                res.PublicKey = Convert.FromBase64String(peer.PublicKey);
            }

            ProtoBuf.Serializer.SerializeWithLengthPrefix<Identify>(stream, res, PrefixStyle.Base128);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        }

        /// <summary>
        ///   Gets the identity information of the remote peer.
        /// </summary>
        /// <param name="connection">
        ///   The currenty connection to the remote peer.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Peer> GetRemotePeerAsync(PeerConnection connection, CancellationToken cancel)
        {
            var muxer = await connection.MuxerEstablished.Task.ConfigureAwait(false);
            log.Debug("Get remote identity");
            Peer remote = connection.RemotePeer;
            if (remote == null)
            {
                remote = new Peer();
                connection.RemotePeer = remote;
            }

            // Read the remote peer identify info.
            using (var stream = await muxer.CreateStreamAsync("id", cancel).ConfigureAwait(false))
            {
                await connection.EstablishProtocolAsync("/multistream/", stream, cancel).ConfigureAwait(false);
                await connection.EstablishProtocolAsync("/ipfs/id/", stream, cancel).ConfigureAwait(false);
                await UpdateRemotePeerAsync(remote, stream, cancel).ConfigureAwait(false);
            }

            // It should always contain the address we used for connections, so
            // that NAT translations are maintained.
            if (connection.RemoteAddress != null && !remote.Addresses.Contains(connection.RemoteAddress))
            {
                var addrs = remote.Addresses.ToList();
                addrs.Add(connection.RemoteAddress);
                remote.Addresses = addrs;
            }

            connection.IdentityEstablished.TrySetResult(remote);

            log.Debug($"Peer id '{remote}' of {connection.RemoteAddress}");
            return remote;
        }

        /// <summary>
        ///   Read the identify message and update the peer information.
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="stream"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task UpdateRemotePeerAsync(Peer remote, Stream stream, CancellationToken cancel)
        {
            var info = await ProtoBufHelper.ReadMessageAsync<Identify>(stream, cancel).ConfigureAwait(false);

            remote.AgentVersion = info.AgentVersion;
            remote.ProtocolVersion = info.ProtocolVersion;
            if (info.PublicKey == null || info.PublicKey.Length == 0)
            {
                throw new InvalidDataException("Public key is missing.");
            }
            remote.PublicKey = Convert.ToBase64String(info.PublicKey);
            if (remote.Id == null)
            {
                remote.Id = MultiHash.ComputeHash(info.PublicKey);
            }

            if (info.ListenAddresses != null)
            {
                remote.Addresses = info.ListenAddresses
                    .Select(b => MultiAddress.TryCreate(b))
                    .Where(a => a != null)
                    .Select(a => a.WithPeerId(remote.Id))
                    .ToList();
            }
            if (!remote.Addresses.Any())
                log.Warn($"No listen address for {remote}");

            if (!remote.IsValid())
            {
                throw new InvalidDataException($"Invalid peer {remote}.");
            }
        }

        [ProtoContract]
        private class Identify
        {
            [ProtoMember(5)]
            public string ProtocolVersion;

            [ProtoMember(6)]
            public string AgentVersion;

            [ProtoMember(1)]
            public byte[] PublicKey;

            [ProtoMember(2, IsRequired = true)]
            public byte[][] ListenAddresses;

            [ProtoMember(4)]
            public byte[] ObservedAddress;

            [ProtoMember(3)]
            public string[] Protocols;
        }
    }
}