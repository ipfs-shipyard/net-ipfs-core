using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipfs.Engine.BlockExchange;

/// <summary>
///     A content addressable block that is wanted by a peer.
/// </summary>
public class WantedBlock
{
    /// <summary>
    ///     The consumers that are waiting for the block.
    /// </summary>
    public List<TaskCompletionSource<IDataBlock>> Consumers;

    /// <summary>
    ///     The content ID of the block;
    /// </summary>
    public Cid Id;

    /// <summary>
    ///     The peers that want the block.
    /// </summary>
    public List<MultiHash> Peers;
}