using McMaster.Extensions.CommandLineUtils;

namespace IpfsShipyard.Ipfs.Cli.Commands;

[Command(Description = "Stop the IPFS deamon")]
internal class ShutdownCommand : CommandBase
{
    private Program Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        await Parent.CoreApi.Generic.ShutdownAsync();
        return 0;
    }
}