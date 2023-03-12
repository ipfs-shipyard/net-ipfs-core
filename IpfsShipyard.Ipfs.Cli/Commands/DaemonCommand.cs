using McMaster.Extensions.CommandLineUtils;

namespace IpfsShipyard.Ipfs.Cli.Commands;

[Command(Description = "Start a long running IPFS deamon")]
internal class DaemonCommand : CommandBase // TODO
{
    private Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        global::IpfsShipyard.Ipfs.Server.Program.Main(Array.Empty<string>());
        return Task.FromResult(0);
    }
}