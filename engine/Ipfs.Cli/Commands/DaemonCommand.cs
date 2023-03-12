using McMaster.Extensions.CommandLineUtils;

namespace Ipfs.Cli.Commands;

[Command(Description = "Start a long running IPFS deamon")]
internal class DaemonCommand : CommandBase // TODO
{
    private Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        Server.Program.Main(Array.Empty<string>());
        return Task.FromResult(0);
    }
}