using Microsoft.AspNetCore.Mvc;

namespace IpfsShipyard.Ipfs.Server;

/// <summary>
///     Simple controller to present the IPFS WebUI
/// </summary>
[Route("webui")]
public class WebUiController : Controller
{
    /// <summary>
    ///     Gets the IPFS WebUI app.
    /// </summary>
    [HttpGet]
    public ActionResult Get()
    {
        return Redirect("/ipfs/QmfQkD8pBSBCBxWEwFSu4XaDVSWK6bjnNuaWZjMyQbyDub");
    }
}