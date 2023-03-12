using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace IpfsShipyard.Ipfs.Cli.Commands;

[Command(Description = "List links")]
internal class LsCommand : CommandBase
{
    [Argument(0, "ipfs-path", "The path to an IPFS object")]
    [Required]
    public string IpfsPath { get; set; }

    private Program Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var node = await Parent.CoreApi.FileSystem.ListFileAsync(IpfsPath);
        return Parent.Output(app, node, (data, writer) =>
        {
            foreach (var link in data.Links)
            {
                writer.WriteLine($"{link.Id.Encode()} {link.Size} {link.Name}");
            }
        });
    }
}