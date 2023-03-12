using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk;
using ProtoBuf;
using Semver;

#pragma warning disable 0649 // disable warning about unassinged fields
#pragma warning disable 0169 // disable warning about unassinged fields

namespace IpfsShipyard.Ipfs.Engine.BlockExchange;

/// <summary>
///     Bitswap Protocol version 1.1.0
/// </summary>
public class Bitswap11 : IBitswapProtocol
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Bitswap11));

    /// <summary>
    ///     The <see cref="Bitswap" /> service.
    /// </summary>
    public Bitswap Bitswap { get; set; }

    /// <inheritdoc />
    public string Name { get; } = "ipfs/bitswap";

    /// <inheritdoc />
    public SemVersion Version { get; } = new(1, 1);

    /// <inheritdoc />
    public async Task ProcessMessageAsync(PeerConnection connection, Stream stream, CancellationToken cancel = default)
    {
        // There is a race condition between getting the remote identity and
        // the remote sending the first wantlist.
        await connection.IdentityEstablished.Task.ConfigureAwait(false);

        while (cancel.IsCancellationRequested)
        {
            var request = await ProtoBufHelper.ReadMessageAsync<Message>(stream, cancel).ConfigureAwait(false);

            // Process want list
            if (request.Wantlist is { Entries: { } })
            {
                foreach (var entry in request.Wantlist.Entries)
                {
                    var cid = Cid.Read(entry.Block);
                    if (entry.Cancel)
                    {
                        // TODO: Unwant specific to remote peer
                        Bitswap.Unwant(cid);
                    }
                    else
                    {
                        // TODO: Should we have a timeout?
                        var _ = GetBlockAsync(cid, connection.RemotePeer, CancellationToken.None);
                    }
                }
            }

            // Forward sent blocks to the block service.  Eventually
            // bitswap will here about and them and then continue
            // any tasks (GetBlockAsync) waiting for the block.
            if (request.Payload == null)
            {
                continue;
            }

            Log.Debug($"got block(s) from {connection.RemotePeer}");
            foreach (var sentBlock in request.Payload)
            {
                using var ms = new MemoryStream(sentBlock.Prefix);
                var version = await ms.ReadVarint32Async(cancel);
                var contentType = ms.ReadMultiCodec().Name;
                var multiHash = MultiHash.GetHashAlgorithmName(await ms.ReadVarint32Async(cancel));
                await Bitswap.OnBlockReceivedAsync(connection.RemotePeer, sentBlock.Data, contentType, multiHash);
            }
        }
        
    }

    /// <inheritdoc />
    public async Task SendWantsAsync(
        Stream stream,
        IEnumerable<WantedBlock> wants,
        bool full = true,
        CancellationToken cancel = default
    )
    {
        var message = new Message
        {
            Wantlist = new()
            {
                Full = full,
                Entries = wants
                    .Select(w => new Entry
                    {
                        Block = w.Id.ToArray()
                    })
                    .ToArray()
            },
            Payload = new(0)
        };

        Serializer.SerializeWithLengthPrefix(stream, message, PrefixStyle.Base128);
        await stream.FlushAsync(cancel).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"/{Name}/{Version}";
    }

    private async Task GetBlockAsync(Cid cid, Peer remotePeer, CancellationToken cancel)
    {
        // TODO: Determine if we will fetch the block for the remote
        try
        {
            IDataBlock block;
            if (null != await Bitswap.BlockService.StatAsync(cid, cancel).ConfigureAwait(false))
            {
                block = await Bitswap.BlockService.GetAsync(cid, cancel).ConfigureAwait(false);
            }
            else
            {
                block = await Bitswap.WantAsync(cid, remotePeer.Id, cancel).ConfigureAwait(false);
            }

            // Send block to remote.
            await using var stream = await Bitswap.Swarm.DialAsync(remotePeer, ToString(), cancel).ConfigureAwait(false);
            await SendAsync(stream, block, cancel).ConfigureAwait(false);

            await Bitswap.OnBlockSentAsync(remotePeer, block).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // eat it
        }
        catch (Exception e)
        {
            Log.Warn("getting block for remote failed", e);
            // eat it.
        }
    }

    internal async Task SendAsync(
        Stream stream,
        IDataBlock block,
        CancellationToken cancel = default
    )
    {
        Log.Debug($"Sending block {block.Id}");
        var message = new Message
        {
            Payload = new()
            {
                new()
                {
                    Prefix = GetCidPrefix(block.Id),
                    Data = block.DataBytes
                }
            }
        };

        Serializer.SerializeWithLengthPrefix(stream, message, PrefixStyle.Base128);
        await stream.FlushAsync(cancel).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the CID "prefix".
    /// </summary>
    /// <param name="id">
    ///     The CID.
    /// </param>
    /// <returns>
    ///     A byte array of consisting of cid version, multicodec and multihash prefix (type + length).
    /// </returns>
    private static byte[] GetCidPrefix(Cid id)
    {
        using var ms = new MemoryStream();
        ms.WriteVarint(id.Version);
        ms.WriteMultiCodec(id.ContentType);
        ms.WriteVarint(id.Hash.Algorithm.Code);
        ms.WriteVarint(id.Hash.Digest.Length);
        return ms.ToArray();
    }

    [ProtoContract]
    private record Entry
    {
        [ProtoMember(1)]
        // changed from string to bytes, it makes a difference in JavaScript
        public byte[] Block; // the block cid (cidV0 in bitswap 1.0.0, cidV1 in bitswap 1.1.0)

        [ProtoMember(3)] public bool Cancel; // whether this revokes an entry

        [ProtoMember(2)] public int Priority = 1; // the priority (normalized). default to 1
    }

    [ProtoContract]
    private record Wantlist
    {
        [ProtoMember(1)] public Entry[] Entries; // a list of wantlist entries

        [ProtoMember(2)] public bool Full; // whether this is the full wantlist. default to false
    }

    [ProtoContract]
    private record Block
    {
        [ProtoMember(2)] public byte[] Data;

        [ProtoMember(1)]
        public byte[] Prefix; // CID prefix (cid version, multicodec and multihash prefix (type + length)
    }

    [ProtoContract]
    private record Message
    {
        [ProtoMember(2)] public byte[][] Blocks; // used to send Blocks in bitswap 1.0.0

        [ProtoMember(3)] public List<Block> Payload; // used to send Blocks in bitswap 1.1.0

        [ProtoMember(1)] public Wantlist Wantlist;
    }
}