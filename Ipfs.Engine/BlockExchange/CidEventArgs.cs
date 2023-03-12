using System;
using IpfsShipyard.Ipfs.Core;

namespace Ipfs.Engine.BlockExchange;

/// <summary>
///     The content addressable ID related to an event.
/// </summary>
/// <see cref="Cid" />
/// <see cref="Bitswap.BlockNeeded" />
public class CidEventArgs : EventArgs
{
    /// <summary>
    ///     The content addressable ID.
    /// </summary>
    /// <value>
    ///     The unique ID of the block.
    /// </value>
    public Cid Id { get; set; }
}