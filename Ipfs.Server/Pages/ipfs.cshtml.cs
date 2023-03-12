using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;

namespace Ipfs.Server.Pages;

/// <summary>
///     An IPFS file or directory.
/// </summary>
public class IpfsModel : PageModel
{
    private ICoreApi _ipfs;
    private IFileSystemNode _node;

    /// <summary>
    ///     Creates a new instance of the controller.
    /// </summary>
    /// <param name="ipfs">
    ///     An object that implements the ICoreApi, typically an IpfsEngine.
    /// </param>
    public IpfsModel(ICoreApi ipfs)
    {
        _ipfs = ipfs;
    }

    /// <summary>
    ///     The IPFS path to a file or directry.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Path { get; set; }

    /// <summary>
    ///     The parts of the <see cref="Path" />.
    /// </summary>
    public IEnumerable<string> PathParts
    {
        get
        {
            return Path
                .Split('/')
                .Where(p => !string.IsNullOrWhiteSpace(p));
        }
    }

    /// <summary>
    ///     The parent path.
    /// </summary>
    public string Parent
    {
        get
        {
            var parts = PathParts.ToList();
            parts.RemoveAt(parts.Count - 1);
            return string.Join('/', parts);
        }
    }

    /// <summary>
    ///     A sequence of files for the directory.
    /// </summary>
    public IEnumerable<IFileSystemLink> Files => _node.Links;

    /// <summary>
    ///     Get the file or directory.
    /// </summary>
    /// <remarks>
    ///     Returns the contents of the file or a page listing the directory.
    /// </remarks>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancel)
    {
        if (string.IsNullOrWhiteSpace(Path))
        {
            return NotFound();
        }

        try
        {
            _node = await _ipfs.FileSystem.ListFileAsync(Path, cancel);
        }
        catch (ArgumentException e)
        {
            return NotFound(e.Message);
        }

        // If a directory, then display a page with directory contents or if
        // the directory contain "index.html", redirect to the page.
        if (_node.IsDirectory)
        {
            if (!Path.EndsWith("/") && _node.Links.Any(l => l.Name == "index.html"))
            {
                return Redirect($"/ipfs/{Path}/index.html");
            }

            Path = Path.TrimEnd('/');
            return Page();
        }

        // If a file, send it.
        var etag = new EntityTagHeaderValue("\"" + _node.Id + "\"", false);
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(Path, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var stream = await _ipfs.FileSystem.ReadFileAsync(_node.Id, cancel);
        Response.Headers.Add("cache-control", new("public, max-age=31536000, immutable"));
        Response.Headers.Add("etag", new(etag.Tag));
        return File(stream, contentType);
    }
}