﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Microsoft.AspNetCore.Mvc;
// TODO: need MultiAddress.WithOutPeer (should be in IPFS code)

namespace Ipfs.Server.HttpApi.V0;

/// <summary>
///     Information from the Distributed Hash Table.
/// </summary>
public class DhtPeerDto
{
    /// <summary>
    ///     Unknown.
    /// </summary>
    public string Extra = string.Empty;

    /// <summary>
    ///     The ID of the peer that provided the response.
    /// </summary>
    public string Id;

    /// <summary>
    ///     The peer that has the information.
    /// </summary>
    public IEnumerable<DhtPeerResponseDto> Responses;

    /// <summary>
    ///     Unknown.
    /// </summary>
    public int Type; // TODO: what is the type?
}

/// <summary>
///     Information on a peer that has the information.
/// </summary>
public class DhtPeerResponseDto
{
    /// <summary>
    ///     The listening addresses of the peer.
    /// </summary>
    public IEnumerable<string> Addrs;

    /// <summary>
    ///     The peer ID.
    /// </summary>
    public string Id;
}

/// <summary>
///     Distributed Hash Table.
/// </summary>
/// <remarks>
///     The DHT is a place to store, not the value, but pointers to peers who have
///     the actual value.
/// </remarks>
public class DhtController : IpfsController
{
    /// <summary>
    ///     Creates a new controller.
    /// </summary>
    public DhtController(ICoreApi ipfs) : base(ipfs)
    {
    }

    /// <summary>
    ///     Query the DHT for all of the multiaddresses associated with a Peer ID.
    /// </summary>
    /// <param name="arg">
    ///     The peer ID to find.
    /// </param>
    /// <returns>
    ///     Information about the peer.
    /// </returns>
    [HttpGet]
    [HttpPost]
    [Route("dht/findpeer")]
    public async Task<DhtPeerDto> FindPeer(string arg)
    {
        var peer = await IpfsCore.Dht.FindPeerAsync(arg, Cancel);
        return new()
        {
            Id = peer.Id.ToBase58(),
            Responses = new DhtPeerResponseDto[]
            {
                new()
                {
                    Id = peer.Id.ToBase58(),
                    Addrs = peer.Addresses.Select(a => a.WithoutPeerId().ToString())
                }
            }
        };
    }

    /// <summary>
    ///     Find peers in the DHT that can provide a specific value, given a key.
    /// </summary>
    /// <param name="arg">
    ///     The CID key,
    /// </param>
    /// <param name="limit">
    ///     The maximum number of providers to find.
    /// </param>
    /// <returns>
    ///     Information about the peer providers.
    /// </returns>
    [HttpGet]
    [HttpPost]
    [Route("dht/findprovs")]
    public async Task<IEnumerable<DhtPeerDto>> FindProviders(
        string arg,
        [ModelBinder(Name = "num-providers")] int limit = 20
    )
    {
        var peers = await IpfsCore.Dht.FindProvidersAsync(arg, limit, null, Cancel);
        return peers.Select(peer => new DhtPeerDto
        {
            Id = peer.Id.ToBase58(), // TODO: should be the peer ID that answered the query
            Responses = new DhtPeerResponseDto[]
            {
                new()
                {
                    Id = peer.Id.ToBase58(),
                    Addrs = peer.Addresses.Select(a => a.WithoutPeerId().ToString())
                }
            }
        });
    }
}