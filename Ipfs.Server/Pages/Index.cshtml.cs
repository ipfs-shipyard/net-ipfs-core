using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ipfs.Server.Pages;

/// <summary>
///     Not used.
/// </summary>
public class IndexModel : PageModel
{
    private ICoreApi _ipfs;

    /// <summary>
    ///     The local node's globally unique identifier.
    /// </summary>
    public string NodeId = "foo-bar";

    /// <summary>
    ///     Creates a new instance of the controller.
    /// </summary>
    public IndexModel(ICoreApi ipfs)
    {
        _ipfs = ipfs;
    }

    /// <summary>
    ///     Build the model.
    /// </summary>
    public async Task OnGetAsync(CancellationToken cancel)
    {
        var peer = await _ipfs.Generic.IdAsync(null, cancel);
        NodeId = peer.Id.ToString();
    }
}