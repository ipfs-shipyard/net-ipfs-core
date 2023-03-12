using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IpfsShipyard.Ipfs.Server.Pages;

/// <summary>
///     Information about the server.
/// </summary>
public class AboutModel : PageModel
{
    /// <summary>
    ///     The message to display.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    ///     Build the model.
    /// </summary>
    public void OnGet()
    {
        Message = "Your application description page.";
    }
}