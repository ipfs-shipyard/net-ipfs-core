using System.IO;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Engine.Tests;

/// <summary>
///     Creates a temporary node.
/// </summary>
/// <remarks>
///     A temporary node has its own repository and listening address.
///     When it is disposed, the repository is deleted.
/// </remarks>
internal class TempNode : IpfsEngine
{
    private static int _nodeNumber;

    public TempNode()
        : base("xyzzy")
    {
        Options.Repository.Folder = Path.Combine(Path.GetTempPath(), $"ipfs-{_nodeNumber++}");
        Options.KeyChain.DefaultKeyType = "ed25519";
        Config.SetAsync(
            "Addresses.Swarm",
            JToken.FromObject(new[] { "/ip4/0.0.0.0/tcp/0" })
        ).Wait();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (Directory.Exists(Options.Repository.Folder))
        {
            Directory.Delete(Options.Repository.Folder, true);
        }
    }
}