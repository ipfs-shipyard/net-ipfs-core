﻿using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace Ipfs.Server.Api.V0
{

    /// <summary>
    ///  A list of peers.
    /// </summary>
    public class BootstrapPeersDto
    {
        /// <summary>
        ///   The multiaddress of a peer.
        /// </summary>
        public IEnumerable<string> Peers;
    }

    /// <summary>
    ///   Manages the list of initial peers.
    /// </summary>
    /// <remarks>
    ///  The API manipulates the "bootstrap list", which contains
    ///  the addresses of the bootstrap nodes. These are the trusted peers from
    ///  which to learn about other peers in the network.
    /// </remarks>
    public class BootstrapController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public BootstrapController(ICoreApi ipfs) : base(ipfs) { }


        /// <summary>
        ///   List all the bootstrap peers.
        /// </summary>
        [HttpGet, HttpPost, Route("bootstrap/list")]
        public async Task<BootstrapPeersDto> List()
        {
            var peers = await IpfsCore.Bootstrap.ListAsync(Timeout.Token);
            return new BootstrapPeersDto
            {
                Peers = peers.Select(peer => peer.ToString())
            };
        }

        /// <summary>
        ///   Remove all the bootstrap peers.
        /// </summary>
        [HttpGet, HttpPost, Route("bootstrap/rm/all")]
        public async Task RemoveAll()
        {
            await IpfsCore.Bootstrap.RemoveAllAsync(Timeout.Token);
        }

        /// <summary>
        ///   Add the default bootstrap peers.
        /// </summary>
        [HttpGet, HttpPost, Route("bootstrap/add/default")]
        public async Task<BootstrapPeersDto> AddDefaults()
        {
            var peers = await IpfsCore.Bootstrap.AddDefaultsAsync(Timeout.Token);
            return new BootstrapPeersDto
            {
                Peers = peers.Select(peer => peer.ToString())
            };
        }

        /// <summary>
        ///   Add a bootstrap peer.
        /// </summary>
        /// <param name="arg">
        ///   The multiaddress of the peer.
        /// </param>
        /// <returns></returns>
        [HttpGet, HttpPost, Route("bootstrap/add")]
        public async Task<BootstrapPeersDto> Add(string arg)
        {
            var peer = await IpfsCore.Bootstrap.AddAsync(arg, Timeout.Token);
            return new BootstrapPeersDto
            {
                Peers = new [] { peer?.ToString() }
            };
        }

        /// <summary>
        ///   Remove a bootstrap peer.
        /// </summary>
        /// <param name="arg">
        ///   The multiaddress of the peer.
        /// </param>
        /// <returns></returns>
        [HttpGet, HttpPost, Route("bootstrap/rm")]
        public async Task<BootstrapPeersDto> Remove(
            string arg,
            bool all = false)
        {
            if (all)
            {
                await IpfsCore.Bootstrap.RemoveAllAsync(Timeout.Token);
                return new BootstrapPeersDto { Peers = new string[0] };
            }

            var peer = await IpfsCore.Bootstrap.RemoveAsync(arg, Timeout.Token);
            return new BootstrapPeersDto
            {
                Peers = new[] { peer?.ToString() }
            };
        }

    }
}
